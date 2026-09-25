using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LGRRS.Api.Auth;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace LGRRS.Api.Controllers;

[ApiController, Route("api/admin/merchants"), Authorize(Roles = Roles.LgaAdmin)]
public class MerchantReviewsController(LgrrsDbContext db) : ControllerBase
{
    public record ReviewRequest([Required] string Action, [Required, StringLength(1000)] string Reason, [Required] string ExpectedStatus);
    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("A reason is required.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await PosLock.Acquire(db, id);
        var m = await db.Merchants.FindAsync(id);
        if (m == null) return NotFound();
        if (m.Status.ToString() != request.ExpectedStatus) return Conflict("Status changed. Reload before reviewing.");
        MerchantStatus next;
        switch (request.Action)
        {
            case "Approve" when m.Status != MerchantStatus.Verified: next = MerchantStatus.Verified; break;
            case "RequestCorrections" when m.Status is MerchantStatus.Pending or MerchantStatus.UnderReview: next = MerchantStatus.UnderReview; break;
            case "Reject" when m.Status is MerchantStatus.Pending or MerchantStatus.UnderReview: next = MerchantStatus.Rejected; break;
            case "Suspend" when m.Status == MerchantStatus.Verified: next = MerchantStatus.Suspended; break;
            default: return Conflict("This action is not available for the current business status.");
        }
        var previous = m.Status.ToString();
        m.Status = next; m.ReviewReason = request.Reason.Trim(); m.ReviewedAt = DateTimeOffset.UtcNow;
        if (next == MerchantStatus.Verified) m.VerifiedAt = m.ReviewedAt;
        else { m.PosApiKeyHash = null; m.PosApiKeyPrefix = null; m.PosApiKeyExpiresAt = null; }
        db.AuditEvents.Add(new AuditEvent { EventType = "MerchantReviewDecision", EntityType = "Merchant", EntityId = id,
            Metadata = JsonSerializer.Serialize(new { Action = request.Action, PreviousStatus = previous, NewStatus = next.ToString(), Reason = m.ReviewReason }) });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return Ok(new { Status = next.ToString(), m.ReviewReason, m.ReviewedAt });
    }
    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> History(Guid id)
    {
        var events = await db.AuditEvents.AsNoTracking().Where(e => e.EntityId == id && e.EntityType == "Merchant" && e.EventType == "MerchantReviewDecision")
            .OrderByDescending(e => e.Timestamp).ToListAsync();
        await AuditActorDisplay.ResolveLegacy(db, events);
        return Ok(events.Select(e => new { e.EventId, e.Timestamp, e.ActorName, e.ActorRole, e.Metadata }));
    }
}
