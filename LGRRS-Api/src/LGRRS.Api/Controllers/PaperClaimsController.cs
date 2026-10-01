using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LGRRS.Api.Auth;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController, Route("api/consumer/paper-receipts"), Authorize(Roles = Roles.Consumer)]
public class PaperClaimsController(LgrrsDbContext db) : ControllerBase
{
    public record ClaimRequest([Required, StringLength(64)] string Code);
    [HttpPost("claim")]
    public async Task<IActionResult> Claim(ClaimRequest request)
    {
        Response.Headers.CacheControl = "no-store";
        var code = request.Code.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
        if (code.Length != 32 || code.Any(c => !Uri.IsHexDigit(c))) return BadRequest("Enter the 32-character code printed on your receipt.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        var phone = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Coordinate with draw publication so a claim cannot enter an already published draw.
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sp_getapplock @Resource='LGRRS.DrawAdministration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'Please retry your claim.', 1;");
        var receipt = await db.Receipts.Include(r => r.Merchant).SingleOrDefaultAsync(r => r.PaperClaimHash == hash);
        if (receipt == null) return NotFound("Receipt code not found.");
        if (receipt.PaperClaimedAt != null)
        {
            if (receipt.CustomerPhoneHash != phone) return Conflict("This receipt has already been claimed.");
            return Ok(new { receipt.ReceiptId, AlreadyClaimed = true, Message = "This receipt is already in your wallet." });
        }
        var now = DateTimeOffset.UtcNow;
        if (receipt.PaperClaimExpiresAt == null || receipt.PaperClaimExpiresAt <= now) return StatusCode(410, "This receipt claim code has expired.");
        if (receipt.Status != ReceiptStatus.Valid || receipt.Merchant!.Status != MerchantStatus.Verified)
            return Conflict("This receipt is not available to claim. Contact support.");
        if (receipt.CustomerPhoneHash != null) return Conflict("This receipt is already linked to a customer.");
        receipt.CustomerPhoneHash = phone;
        receipt.CustomerPhoneMasked = User.FindFirstValue(ClaimTypes.Name) ?? "Verified customer";
        receipt.PaperClaimedAt = now;
        var draw = await db.DrawPeriods.Where(d => d.Status == DrawPeriodStatus.Open && (d.LgaCode == null || d.LgaCode == receipt.Merchant.LgaCode.Trim().ToUpper()) &&
            d.StartDate <= receipt.TransactionDate && d.EndDate >= receipt.TransactionDate && d.EndDate >= now)
            .OrderBy(d => d.EndDate).FirstOrDefaultAsync();
        if (draw != null)
        {
            receipt.DrawPeriodId = draw.DrawPeriodId;
            await ReceiptsController.AddRewardEntry(db, receipt.Merchant, receipt, draw);
        }
        db.AuditEvents.Add(new AuditEvent { EventType = "PaperReceiptClaimed", EntityType = "Receipt",
            EntityId = receipt.ReceiptId, ActorId = phone, Metadata = "Verified customer linked paper receipt." });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { receipt.ReceiptId, AlreadyClaimed = false,
            Message = "Receipt added to your wallet. Check your wallet for reward eligibility." });
    }
}
