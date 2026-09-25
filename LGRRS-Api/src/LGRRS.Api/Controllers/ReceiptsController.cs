using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LGRRS.Api.Auth;
using LGRRS.Api.Contracts;
using LGRRS.Domain;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using LGRRS.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController]
[Route("api/merchant/receipts")]
[Authorize(Roles = Roles.Merchant)]
public class ReceiptsController : ControllerBase
{
    private readonly LgrrsDbContext _db;
    private readonly IDataProtectionCodec _codec;
    private readonly IConfiguration _config;

    public ReceiptsController(LgrrsDbContext db, IDataProtectionCodec codec, IConfiguration config)
    {
        _db = db;
        _codec = codec;
        _config = config;
    }

    private Guid CurrentMerchantId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<ReceiptHistoryItem>> Create(CreateReceiptRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        await PosLock.Acquire(_db, CurrentMerchantId);
        var result = await CreateForMerchant(request, CurrentMerchantId);
        if (result.Result is OkObjectResult) await tx.CommitAsync();
        return result;
    }

    public record PosReceiptRequest(
        [Required, StringLength(100)] string ExternalSaleId,
        [Required] PosSaleReceipt Receipt);

    public record PosSaleReceipt(string? CustomerName, string? CustomerPhone,
        string ItemService, decimal Amount, List<string>? DeliveryChannels);

    [AllowAnonymous]
    [HttpPost("/api/pos/receipts")]
    [RequestSizeLimit(32768)]
    public async Task<IActionResult> SubmitPos(PosReceiptRequest request)
    {
        Response.Headers.CacheControl = "no-store";
        var key = Request.Headers["X-API-Key"].ToString();
        if (key.Length != 74 || !key.StartsWith("lgrrs_pos_", StringComparison.Ordinal))
            return Unauthorized("Invalid API key.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        var merchantId = await _db.Merchants.AsNoTracking().Where(m => m.PosApiKeyHash == hash)
            .Select(m => (Guid?)m.MerchantId).SingleOrDefaultAsync();
        if (merchantId == null) return Unauthorized("Invalid API key.");
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await PosLock.Acquire(_db, merchantId.Value);
        var merchant = await _db.Merchants.AsNoTracking().SingleAsync(m => m.MerchantId == merchantId);
        if (merchant.PosApiKeyHash != hash || merchant.PosApiKeyExpiresAt is null || merchant.PosApiKeyExpiresAt <= DateTimeOffset.UtcNow)
            return Unauthorized("Invalid or expired API key.");
        if (!merchant.PosIntegrationEnabled || merchant.Status != MerchantStatus.Verified)
            return StatusCode(403, "POS submission is disabled for this business.");
        var saleId = request.ExternalSaleId.Trim();
        HttpContext.Items["PosAuditMerchant"] = merchant;
        var receiptRequest = new CreateReceiptRequest(request.Receipt.CustomerName,
            request.Receipt.CustomerPhone, request.Receipt.ItemService, request.Receipt.Amount,
            request.Receipt.DeliveryChannels ?? []);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(receiptRequest, jsonOptions))));
        var existing = await _db.PosSubmissions.SingleOrDefaultAsync(s => s.MerchantId == merchantId && s.ExternalSaleId == saleId);
        if (existing != null)
        {
            if (existing.PayloadHash != payloadHash) return Conflict("This sale ID was already used with different receipt details.");
            Response.Headers["X-Idempotent-Replay"] = "true";
            return Content(existing.ResponseJson.StartsWith("protected:") ? _codec.Decrypt(existing.ResponseJson[10..]) : existing.ResponseJson, "application/json");
        }
        var result = await CreateForMerchant(receiptRequest, merchantId.Value, allowPaperReceipt: true);
        if (result.Result is not OkObjectResult success) return result.Result!;
        var responseJson = JsonSerializer.Serialize(success.Value, jsonOptions);
        _db.PosSubmissions.Add(new PosSubmission { MerchantId = merchantId.Value,
            ExternalSaleId = saleId, PayloadHash = payloadHash, ResponseJson = "protected:" + _codec.Encrypt(responseJson) });
        _db.AuditEvents.Add(new AuditEvent { EventType = "PosReceiptAccepted", EntityType = "Merchant",
            EntityId = merchantId.Value, ActorId = merchantId.ToString(), Metadata = saleId });
        await _db.SaveChangesAsync(); await transaction.CommitAsync();
        return Content(responseJson, "application/json");
    }

    private async Task<ActionResult<ReceiptHistoryItem>> CreateForMerchant(CreateReceiptRequest request, Guid merchantId, bool allowPaperReceipt = false)
    {
        if (request.Amount <= 0 || request.Amount > 1000000000m || decimal.Round(request.Amount, 2) != request.Amount ||
            string.IsNullOrWhiteSpace(request.ItemService) || request.ItemService.Length > 200)
            return BadRequest("Enter a description and a positive amount with at most two decimal places.");
        if (request.DeliveryChannels is null || (!allowPaperReceipt && request.DeliveryChannels.Count == 0) ||
            request.DeliveryChannels.Any(c => c is not ("WHATSAPP" or "SMS" or "IN_APP")) ||
            request.DeliveryChannels.Distinct().Count() != request.DeliveryChannels.Count)
            return BadRequest("Choose at least one valid, distinct delivery channel.");
        var hasPhone = !string.IsNullOrWhiteSpace(request.CustomerPhone);
        if (!hasPhone && (!allowPaperReceipt || request.DeliveryChannels.Count > 0))
            return BadRequest("A customer phone number is required for digital delivery. For a POS paper receipt, omit the phone and use an empty deliveryChannels list.");
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant is null)
        {
            return NotFound();
        }

        if (merchant.Status != MerchantStatus.Verified)
        {
            return Conflict("Only verified merchants may issue reward-eligible receipts.");
        }

        if (request.DeliveryChannels.Any(c => c is "WHATSAPP" or "SMS") && string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            return BadRequest("Customer phone number is required for WhatsApp/SMS delivery.");
        }

        var now = DateTimeOffset.UtcNow;
        var currentDraw = await _db.DrawPeriods.Where(d => d.Status == DrawPeriodStatus.Open &&
            d.StartDate <= now && d.EndDate >= now).OrderBy(d => d.EndDate).FirstOrDefaultAsync();

        var receipt = new Receipt
        {
            MerchantId = merchant.MerchantId,
            CustomerName = request.CustomerName,
            ItemService = request.ItemService,
            Amount = request.Amount,
            Status = ReceiptStatus.Valid,
            DrawPeriodId = hasPhone ? currentDraw?.DrawPeriodId : null,
            TransactionDate = DateTimeOffset.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            receipt.CustomerPhoneEncrypted = _codec.Encrypt(request.CustomerPhone);
            receipt.CustomerPhoneHash = _codec.Hash(request.CustomerPhone);
            receipt.CustomerPhoneMasked = PhoneMasker.Mask(request.CustomerPhone);
        }

        string? claimCode = null;
        string? claimUrl = null;
        if (allowPaperReceipt && !hasPhone)
        {
            claimCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            receipt.PaperClaimHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(claimCode)));
            receipt.PaperClaimExpiresAt = now.AddDays(30);
            var origin = (_config["PublicWebUrl"] ?? "http://127.0.0.1:5173").TrimEnd('/');
            claimUrl = $"{origin}/check/claim#code={claimCode}";
        }
        _db.Receipts.Add(receipt);

        foreach (var channel in request.DeliveryChannels)
        {
            _db.ReceiptDeliveries.Add(new ReceiptDelivery
            {
                ReceiptId = receipt.ReceiptId,
                Channel = ParseChannel(channel),
                DestinationMasked = receipt.CustomerPhoneMasked,
                Status = DeliveryStatus.Sent,
                SentAt = DateTimeOffset.UtcNow
            });
        }

        _db.AuditEvents.Add(new AuditEvent
        {
            EventType = "ReceiptIssued",
            EntityType = "Receipt",
            EntityId = receipt.ReceiptId,
            ActorId = merchant.MerchantId.ToString(),
            Metadata = receipt.ReceiptRefMasked
        });

        if (hasPhone && currentDraw is not null && receipt.Status == ReceiptStatus.Valid)
        {
            await AddRewardEntry(_db, merchant, receipt, currentDraw);
        }

        await _db.SaveChangesAsync();

        return Ok(new ReceiptHistoryItem(
            receipt.ReceiptRefMasked,
            receipt.CustomerName,
            receipt.CustomerPhoneMasked,
            receipt.ItemService,
            receipt.Amount,
            request.DeliveryChannels.Count == 0 ? "RECORDED" : "SENT",
            request.DeliveryChannels,
            receipt.TransactionDate, claimCode, claimUrl));
    }

    internal static async Task AddRewardEntry(LgrrsDbContext _db, Merchant merchant, Receipt receipt, DrawPeriod currentDraw)
    {
            var entry = new RewardEntry
            {
                ReceiptId = receipt.ReceiptId,
                DrawPeriodId = currentDraw.DrawPeriodId,
                EligibilityStatus = EligibilityStatus.Eligible,
                RiskStatus = RiskStatus.Clear
            };

            // MERCHANT_SELF_ENTRY: a merchant entering purchases against their own phone number.
            if (!string.IsNullOrEmpty(receipt.CustomerPhoneHash) && receipt.CustomerPhoneHash == merchant.PhoneHash)
            {
                entry.EligibilityStatus = EligibilityStatus.Blocked;
                entry.RiskStatus = RiskStatus.Flagged;
                _db.FraudFlags.Add(new FraudFlag
                {
                    EntityType = FraudEntityType.RewardEntry,
                    EntityId = entry.EntryId,
                    RuleCode = FraudRuleCode.MerchantSelfEntry,
                    Severity = FraudSeverity.High,
                    Status = FraudFlagStatus.Open
                });
                _db.AuditEvents.Add(new AuditEvent
                {
                    EventType = "FraudFlagRaised",
                    EntityType = "RewardEntry",
                    EntityId = entry.EntryId,
                    Metadata = "MERCHANT_SELF_ENTRY"
                });
            }
            // REPEATED_VALUE_CLUSTER: a customer splitting a purchase into many small receipts
            // (across any merchant) to rack up cheap, low-friction small-tier entries.
            else if (!string.IsNullOrEmpty(receipt.CustomerPhoneHash) && receipt.Amount <= PrizeTiers.SmallMax)
            {
                var windowStart = DateTimeOffset.UtcNow.AddHours(-24);
                var recentSmallReceipts = await _db.Receipts.CountAsync(r =>
                    r.ReceiptId != receipt.ReceiptId && r.CustomerPhoneHash == receipt.CustomerPhoneHash &&
                    r.Amount <= PrizeTiers.SmallMax &&
                    r.TransactionDate >= windowStart);

                if (recentSmallReceipts >= 2)
                {
                    entry.RiskStatus = RiskStatus.Flagged;
                    _db.FraudFlags.Add(new FraudFlag
                    {
                        EntityType = FraudEntityType.RewardEntry,
                        EntityId = entry.EntryId,
                        RuleCode = FraudRuleCode.RepeatedValueCluster,
                        Severity = FraudSeverity.Medium,
                        Status = FraudFlagStatus.Open
                    });
                    _db.AuditEvents.Add(new AuditEvent
                    {
                        EventType = "FraudFlagRaised",
                        EntityType = "RewardEntry",
                        EntityId = entry.EntryId,
                        Metadata = "REPEATED_VALUE_CLUSTER"
                    });
                }
            }

            _db.RewardEntries.Add(entry);

            _db.AuditEvents.Add(new AuditEvent
            {
                EventType = "RewardEntryCreated",
                EntityType = "RewardEntry",
                EntityId = entry.EntryId,
                ActorId = merchant.MerchantId.ToString(),
                Metadata = $"RR-{entry.EntryId.ToString("N")[..6].ToUpperInvariant()}"
            });
    }

    [HttpGet]
    public async Task<ActionResult<List<ReceiptHistoryItem>>> History()
    {
        var receipts = await _db.Receipts
            .Where(r => r.MerchantId == CurrentMerchantId)
            .Include(r => r.Deliveries)
            .OrderByDescending(r => r.TransactionDate)
            .Take(100)
            .ToListAsync();

        var result = receipts.Select(r => new ReceiptHistoryItem(
            r.ReceiptRefMasked,
            r.CustomerName,
            r.CustomerPhoneMasked,
            r.ItemService,
            r.Amount,
            r.Deliveries.OrderByDescending(d => d.SentAt).FirstOrDefault()?.Status.ToString() ?? "RECORDED",
            r.Deliveries.Select(d => d.Channel.ToString()).ToList(),
            r.TransactionDate));

        return Ok(result);
    }

    private static DeliveryChannel ParseChannel(string raw) => raw.ToUpperInvariant() switch
    {
        "WHATSAPP" => DeliveryChannel.WhatsApp,
        "SMS" => DeliveryChannel.Sms,
        _ => DeliveryChannel.InApp
    };
}
