using System.Security.Claims;
using LGRRS.Api.Auth;
using LGRRS.Api.Contracts;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using LGRRS.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController]
[Route("api/public/prize-claims")]
public class ClaimsController : ControllerBase
{
    private readonly LgrrsDbContext _db;
    private readonly IDataProtectionCodec _codec;

    public ClaimsController(LgrrsDbContext db, IDataProtectionCodec codec)
    {
        _db = db;
        _codec = codec;
    }

    [HttpPost]
    [Authorize(Roles = Roles.Consumer)]
    public async Task<ActionResult<ClaimSubmittedResponse>> Submit(SubmitClaimRequest request)
    {
        var consumerPhoneHash = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var receipt = await _db.Receipts
            .Include(r => r.RewardEntry)
            .ThenInclude(e => e!.DrawResult)
            .ThenInclude(d => d!.PrizeClaim)
            .FirstOrDefaultAsync(r => r.ReceiptId == request.ReceiptId);

        var drawResult = receipt?.RewardEntry?.DrawResult;
        if (receipt is null || drawResult is null)
        {
            return BadRequest("This ticket has not won a prize.");
        }

        if (receipt.CustomerPhoneHash != consumerPhoneHash)
        {
            _db.FraudFlags.Add(new FraudFlag
            {
                EntityType = FraudEntityType.PrizeClaim,
                EntityId = drawResult.DrawResultId,
                RuleCode = FraudRuleCode.ClaimPhoneMismatch,
                Severity = FraudSeverity.High,
                Status = FraudFlagStatus.Open
            });
            _db.AuditEvents.Add(new AuditEvent
            {
                EventType = "FraudFlagRaised",
                EntityType = "PrizeClaim",
                EntityId = drawResult.DrawResultId,
                Metadata = "CLAIM_PHONE_MISMATCH"
            });
            await _db.SaveChangesAsync();
            return StatusCode(403, "This phone number doesn't match the phone number verified for this receipt. Prizes can only be claimed by the phone number used on the original purchase.");
        }

        if (drawResult.PrizeClaim is not null)
        {
            return Conflict("A claim has already been submitted for this winning ticket.");
        }

        if (receipt.Status != ReceiptStatus.Valid ||
            receipt.RewardEntry!.EligibilityStatus != EligibilityStatus.Eligible ||
            receipt.RewardEntry.RiskStatus != RiskStatus.Clear ||
            drawResult.DrawTimestamp.AddDays(7) < DateTimeOffset.UtcNow)
            return Conflict("This receipt is not currently eligible for a prize claim.");

        var claim = new PrizeClaim
        {
            DrawResultId = drawResult.DrawResultId,
            NameEncrypted = _codec.Encrypt(request.FullName),
            PhoneHash = consumerPhoneHash,
            BankNameEncrypted = _codec.Encrypt(request.BankName),
            AccountNumberEncrypted = _codec.Encrypt(request.AccountNumber),
            Status = ClaimStatus.UnderReview,
            ClaimRef = GenerateClaimRef()
        };

        _db.PrizeClaims.Add(claim);
        _db.AuditEvents.Add(new AuditEvent
        {
            EventType = "ClaimSubmitted",
            EntityType = "PrizeClaim",
            EntityId = claim.ClaimId,
            Metadata = claim.ClaimRef
        });
        await _db.SaveChangesAsync();

        return Ok(new ClaimSubmittedResponse(claim.ClaimRef, claim.Status.ToString()));
    }

    [HttpGet("{claimRef}")]
    public async Task<ActionResult<ClaimStatusResponse>> Status(string claimRef)
    {
        var claim = await _db.PrizeClaims.FirstOrDefaultAsync(c => c.ClaimRef == claimRef);
        if (claim is null)
        {
            return NotFound();
        }

        return Ok(new ClaimStatusResponse(claim.ClaimRef, claim.Status.ToString()));
    }

    private static string GenerateClaimRef() => $"CLM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
