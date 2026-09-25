using LGRRS.Api.Auth;
using System.Security.Claims;
using LGRRS.Api.Contracts;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly LgrrsDbContext _db;

    public PublicController(LgrrsDbContext db)
    {
        _db = db;
    }

    // Consumers verify their own phone via OTP (see AuthController) before checking any
    // ticket, so a check is always tied to a signed-in phone rather than fully anonymous.
    [HttpGet("receipts/{receiptId:guid}/check")]
    [Authorize(Roles = Roles.Consumer)]
    public async Task<ActionResult<PublicTicketCheckResponse>> Check(Guid receiptId)
    {
        var receipt = await _db.Receipts
            .Include(r => r.Merchant)
            .Include(r => r.RewardEntry)
            .ThenInclude(e => e!.DrawPeriod)
            .Include(r => r.RewardEntry)
            .ThenInclude(e => e!.DrawResult)
            .ThenInclude(d => d!.PrizeClaim)
            .FirstOrDefaultAsync(r => r.ReceiptId == receiptId &&
                r.CustomerPhoneHash == User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (receipt is null)
        {
            return Ok(new PublicTicketCheckResponse(false, "NOT_FOUND", null, false, null, false, null, null, null, null, null, null, null));
        }

        var businessName = receipt.Merchant?.BusinessName;
        var lgaName = receipt.Merchant?.LgaCode;

        if (receipt.Status is ReceiptStatus.Voided or ReceiptStatus.Refunded)
        {
            return Ok(new PublicTicketCheckResponse(true, "VOIDED_OR_REFUNDED", null, false, null, false, businessName, lgaName, receipt.TransactionDate, receipt.Amount, null, null, null));
        }

        if (receipt.Status == ReceiptStatus.UnderReview)
        {
            return Ok(new PublicTicketCheckResponse(true, "UNDER_REVIEW", null, false, null, false, businessName, lgaName, receipt.TransactionDate, receipt.Amount, null, null, null));
        }

        var entry = receipt.RewardEntry;
        if (entry is null)
        {
            return Ok(new PublicTicketCheckResponse(true, "VALID_NO_ENTRY", "NONE", false, null, false, businessName, lgaName, receipt.TransactionDate, receipt.Amount, null, null, null));
        }

        var entryRef = FormatEntryRef(entry.EntryId);
        var drawName = entry.DrawPeriod is null ? null : FormatDrawName(entry.DrawPeriod);
        if (entry.EligibilityStatus != EligibilityStatus.Eligible || entry.RiskStatus != RiskStatus.Clear)
            return Ok(new PublicTicketCheckResponse(true, "UNDER_REVIEW", "NOT_ELIGIBLE", false, null, false,
                businessName, lgaName, receipt.TransactionDate, receipt.Amount, entryRef, drawName, null));

        var drawResult = entry.DrawResult;
        if (drawResult is not null)
        {
            var claimDeadline = drawResult.DrawTimestamp.AddDays(7);
            return Ok(new PublicTicketCheckResponse(
                true, "VALID", "COMPLETED", true, drawResult.PrizeAmount,
                drawResult.PrizeClaim == null && claimDeadline >= DateTimeOffset.UtcNow,
                businessName, lgaName, receipt.TransactionDate, receipt.Amount, entryRef, drawName, claimDeadline));
        }

        return Ok(new PublicTicketCheckResponse(true, "VALID",
            entry.DrawPeriod?.Status == DrawPeriodStatus.Published ? "COMPLETED" : "PENDING",
            false, null, false, businessName, lgaName, receipt.TransactionDate, receipt.Amount, entryRef, drawName, null));
    }

    [HttpGet("draw-periods/current")]
    public async Task<IActionResult> CurrentDraw()
    {
        var draw = await _db.DrawPeriods
            .Where(d => d.Status == DrawPeriodStatus.Open)
            .OrderByDescending(d => d.StartDate)
            .FirstOrDefaultAsync();

        if (draw is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            draw.DrawPeriodId,
            draw.Type,
            draw.StartDate,
            draw.EndDate,
            draw.DrawDate,
            Status = draw.Status.ToString(),
            draw.PrizeBudget
        });
    }

    private static string FormatEntryRef(Guid entryId) => $"RR-{entryId.ToString("N")[..6].ToUpperInvariant()}";

    private static string FormatDrawName(DrawPeriod draw)
    {
        var month = draw.StartDate.ToString("MMMM yyyy");
        var type = draw.Type == DrawPeriodType.Weekly ? "Weekly Draw" : "Monthly Draw";
        return $"{month} Draw ({type})";
    }
}
