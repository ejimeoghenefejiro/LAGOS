using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using LGRRS.Api.Auth;
using LGRRS.Api.Contracts;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using LGRRS.Infrastructure.Providers;
using LGRRS.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController]
[Route("api/merchants")]
public class MerchantsController : ControllerBase
{
    private readonly LgrrsDbContext _db;
    private readonly IDataProtectionCodec _codec;
    private readonly IBusinessVerificationProvider _verificationProvider;

    public MerchantsController(LgrrsDbContext db, IDataProtectionCodec codec, IBusinessVerificationProvider verificationProvider)
    {
        _db = db;
        _codec = codec;
        _verificationProvider = verificationProvider;
    }

    [HttpPost("register")]
    public async Task<ActionResult<MerchantSummaryResponse>> Register(RegisterMerchantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName) || request.BusinessName.Length > 200 ||
            string.IsNullOrWhiteSpace(request.BusinessType) || request.BusinessType.Length > 100 ||
            string.IsNullOrWhiteSpace(request.LgaCode) || request.LgaCode.Length > 50 ||
            string.IsNullOrWhiteSpace(request.PhoneNumber) || request.PhoneNumber.Length > 30)
        {
            return BadRequest("Enter a business name, business type, LGA and phone number.");
        }
        if (string.IsNullOrWhiteSpace(request.BusinessAddress) || request.BusinessAddress.Length > 500)
            return BadRequest("Enter a business address of at most 500 characters.");
        if (!string.IsNullOrWhiteSpace(request.LgrrsSystemId))
            return BadRequest("LGRRS IDs are assigned automatically. Leave the system ID empty.");
        if (request.LagosTaxId?.Length > 100)
            return BadRequest("The tax ID is too long.");

        var phoneHash = _codec.Hash(request.PhoneNumber);

        var merchant = new Merchant
        {
            BusinessName = request.BusinessName.Trim(),
            BusinessAddress = request.BusinessAddress?.Trim(),
            BusinessType = request.BusinessType.Trim(),
            LgaCode = request.LgaCode.Trim(),
            LagosTaxIdEncrypted = string.IsNullOrWhiteSpace(request.LagosTaxId) ? null : _codec.Encrypt(request.LagosTaxId.Trim()),
            PhoneEncrypted = _codec.Encrypt(request.PhoneNumber),
            PhoneHash = phoneHash,
            Status = MerchantStatus.Pending
        };

        _db.Merchants.Add(merchant);
        // The existing unique database index is authoritative, including concurrent registrations.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            merchant.LgrrsSystemId = $"LGRRS-{RandomNumberGenerator.GetInt32(100000, 1000000)}";
            try { await _db.SaveChangesAsync(); break; }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                if (attempt == 9)
                    return StatusCode(503, "Could not allocate a business ID. Please try again.");
            }
        }

        return Ok(new MerchantSummaryResponse(merchant.MerchantId, merchant.BusinessName, merchant.BusinessType, merchant.LgaCode, merchant.Status.ToString(), merchant.LgrrsSystemId));
    }

    // Approval is only available through the authenticated administrator review endpoint.
    [HttpPost("verify")]
    public IActionResult Verify(Guid merchantId) => StatusCode(410, "Business approval requires administrator review. Sign in to track your application.");

}

[ApiController]
[Route("api/merchant/profile")]
[Authorize(Roles = Roles.Merchant)]
public class MerchantProfileController : ControllerBase
{
    private readonly LgrrsDbContext _db;

    public MerchantProfileController(LgrrsDbContext db)
    {
        _db = db;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> Settings([FromServices] IDataProtectionCodec codec)
    {
        var merchantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var merchant = await _db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant is null) return NotFound();
        return Ok(new {
            merchant.MerchantId, merchant.BusinessName, merchant.BusinessType, merchant.LgaCode,
            merchant.LgrrsSystemId, Status = merchant.Status.ToString(),
            PhoneNumber = codec.Decrypt(merchant.PhoneEncrypted),
            HasTaxId = !string.IsNullOrEmpty(merchant.LagosTaxIdEncrypted),
            merchant.CreatedAt, merchant.VerifiedAt, merchant.PosIntegrationEnabled,
            merchant.BusinessAddress, merchant.ReviewReason, merchant.ReviewedAt,
            PosConnectorAvailable = true
        });
    }

    public record PosPreferenceRequest(bool Enabled);
    public record UpdateDetailsRequest(string BusinessName, string BusinessType, string LgaCode, string BusinessAddress);
    [HttpPatch("settings/details")]
    public async Task<IActionResult> UpdateDetails(UpdateDetailsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName) || request.BusinessName.Length > 200 ||
            string.IsNullOrWhiteSpace(request.BusinessType) || request.BusinessType.Length > 100 ||
            string.IsNullOrWhiteSpace(request.LgaCode) || request.LgaCode.Length > 50 ||
            string.IsNullOrWhiteSpace(request.BusinessAddress) || request.BusinessAddress.Length > 500)
            return BadRequest("Enter a business name, type, LGA and address within the field limits.");
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var tx = await _db.Database.BeginTransactionAsync();
        await PosLock.Acquire(_db, id);
        var m = await _db.Merchants.FindAsync(id);
        if (m == null) return NotFound();
        if (m.Status is not (MerchantStatus.Pending or MerchantStatus.UnderReview)) return Conflict("Profile corrections are available while awaiting review.");
        m.BusinessName = request.BusinessName.Trim(); m.BusinessType = request.BusinessType.Trim();
        m.LgaCode = request.LgaCode.Trim(); m.BusinessAddress = request.BusinessAddress.Trim(); m.Status = MerchantStatus.Pending;
        _db.AuditEvents.Add(new AuditEvent { EventType = "MerchantCorrectionsSubmitted", EntityType = "Merchant", EntityId = id, Metadata = "Business details updated and submitted for review." });
        await _db.SaveChangesAsync(); await tx.CommitAsync();
        return NoContent();
    }

    [HttpPatch("settings/pos")]
    public async Task<IActionResult> SetPosPreference(PosPreferenceRequest request)
    {
        var merchantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await PosLock.Acquire(_db, merchantId);
        var merchant = await _db.Merchants.FindAsync(merchantId);
        if (merchant is null) return NotFound();
        if (merchant.PosIntegrationEnabled != request.Enabled)
        {
            merchant.PosIntegrationEnabled = request.Enabled;
            if (!request.Enabled)
            {
                merchant.PosApiKeyHash = null;
                merchant.PosApiKeyPrefix = null;
                merchant.PosApiKeyExpiresAt = null;
            }
            _db.AuditEvents.Add(new AuditEvent {
                EventType = "PosPreferenceChanged", EntityType = "Merchant",
                EntityId = merchantId, ActorId = merchantId.ToString(),
                Metadata = request.Enabled ? "POS opt-in enabled; not connected." : "POS opt-in disabled."
            });
            await _db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return Ok(new { merchant.PosIntegrationEnabled, PosConnectorAvailable = true });
    }

    [HttpGet]
    public async Task<ActionResult<MerchantSummaryResponse>> Profile()
    {
        var merchantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant is null)
        {
            return NotFound();
        }

        return Ok(new MerchantSummaryResponse(merchant.MerchantId, merchant.BusinessName, merchant.BusinessType, merchant.LgaCode, merchant.Status.ToString(), merchant.LgrrsSystemId));
    }
}


