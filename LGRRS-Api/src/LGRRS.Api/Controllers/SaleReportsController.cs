using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LGRRS.Api.Auth;
using LGRRS.Domain.Entities;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

public record SubmitSaleReport(
    [Required, StringLength(200)] string BusinessName,
    [Required, StringLength(300)] string BusinessLocation,
    [Required, StringLength(50)] string LgaCode,
    DateTimeOffset PurchaseDate,
    [Range(typeof(decimal), "0.01", "1000000000")] decimal Amount,
    [Required] string Issue,
    [Required, StringLength(1000)] string Details);
public record ReviewSaleReport([Required] string Status, [Required, StringLength(1000)] string Note);

[ApiController]
[Route("api/sale-reports")]
public class SaleReportsController(LgrrsDbContext db) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Consumer)]
    public async Task<IActionResult> Submit(SubmitSaleReport request)
    {
        var phone = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var now = DateTimeOffset.UtcNow;
        if (request.PurchaseDate > now || request.PurchaseDate < now.AddDays(-90))
            return BadRequest("Purchase date must be within the last 90 days.");
        if (request.Issue is not ("MissingReceipt" or "IncorrectAmount" or "UnrecognisedReceipt"))
            return BadRequest("Choose a valid receipt issue.");
        if (decimal.Round(request.Amount, 2) != request.Amount)
            return BadRequest("Enter an amount with at most two decimal places.");
        var identity = JsonSerializer.Serialize(new { phone,
            business = request.BusinessName.Trim().ToUpperInvariant(),
            location = request.BusinessLocation.Trim().ToUpperInvariant(),
            date = request.PurchaseDate.UtcDateTime.Date, request.Amount });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        if (await db.SaleReports.AnyAsync(r => r.Fingerprint == fingerprint))
            return Conflict("You have already reported this purchase.");
        if (await db.SaleReports.CountAsync(r => r.ReporterPhoneHash == phone && r.CreatedAt >= now.AddDays(-1)) >= 10)
            return StatusCode(429, "You can submit up to 10 reports per day.");
        var report = new SaleReport {
            ReporterPhoneHash = phone, Fingerprint = fingerprint,
            BusinessName = request.BusinessName.Trim(), BusinessLocation = request.BusinessLocation.Trim(),
            LgaCode = request.LgaCode.Trim(), PurchaseDate = request.PurchaseDate,
            Amount = request.Amount, Issue = request.Issue, Details = request.Details.Trim()
        };
        db.SaleReports.Add(report);
        db.AuditEvents.Add(new AuditEvent { EventType = "SaleReportSubmitted", EntityType = "SaleReport",
            EntityId = report.SaleReportId, ActorId = phone, Metadata = "Unverified customer report; excluded from receipt totals and draws." });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            if (await db.SaleReports.AsNoTracking().AnyAsync(r => r.Fingerprint == fingerprint))
                return Conflict("You have already reported this purchase.");
            throw;
        }
        return Ok(new { report.SaleReportId, report.Status });
    }

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Consumer)]
    public async Task<IActionResult> Mine()
    {
        var phone = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await db.SaleReports.AsNoTracking().Where(r => r.ReporterPhoneHash == phone)
            .OrderByDescending(r => r.CreatedAt).Take(100)
            .Select(r => new { r.SaleReportId, r.BusinessName, r.Amount, r.Issue, r.Status, r.CreatedAt }).ToListAsync());
    }

    [HttpGet]
    [Authorize(Roles = Roles.LgaAdmin)]
    public async Task<IActionResult> Queue() => Ok(await db.SaleReports.AsNoTracking()
        .OrderBy(r => r.Status == "Submitted" ? 0 : r.Status == "Reviewing" ? 1 : 2)
        .ThenByDescending(r => r.CreatedAt).Take(200)
        .Select(r => new { r.SaleReportId, r.BusinessName, r.BusinessLocation, r.LgaCode,
            r.Amount, r.PurchaseDate, r.Issue, r.Details, r.Status, r.ReviewNote, r.CreatedAt }).ToListAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.LgaAdmin)]
    public async Task<IActionResult> Detail(Guid id)
    {
        var report = await db.SaleReports.AsNoTracking().Where(r => r.SaleReportId == id)
            .Select(r => new { r.SaleReportId, r.BusinessName, r.BusinessLocation, r.LgaCode,
                r.Amount, r.PurchaseDate, r.Issue, r.Details, r.Status, r.ReviewNote, r.CreatedAt, r.ReviewedAt })
            .SingleOrDefaultAsync();
        if (report == null) return NotFound("Report not found.");
        var history = await db.AuditEvents.AsNoTracking().Where(a => a.EntityType == "SaleReport" && a.EntityId == id)
            .OrderBy(a => a.Timestamp).ToListAsync();
        await AuditActorDisplay.ResolveLegacy(db, history);
        return Ok(new { Report = report, History = history.Select(a => new { a.EventId, a.EventType, a.Timestamp, a.Metadata, a.ActorName, a.ActorRole }) });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = Roles.LgaAdmin)]
    public async Task<IActionResult> Review(Guid id, ReviewSaleReport request)
    {
        var report = await db.SaleReports.FindAsync(id);
        if (report == null) return NotFound();
        var allowed = report.Status == "Submitted" ? request.Status == "Reviewing"
            : report.Status == "Reviewing" && request.Status is "Resolved" or "Dismissed";
        if (!allowed) return Conflict("Start review before resolving or dismissing a report. Closed reports cannot be changed.");
        report.Status = request.Status;
        report.ReviewNote = request.Note.Trim();
        report.ReviewedAt = DateTimeOffset.UtcNow;
        db.AuditEvents.Add(new AuditEvent { EventType = "SaleReportReviewed", EntityType = "SaleReport",
            EntityId = id, ActorId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Metadata = JsonSerializer.Serialize(new { request.Status, Note = report.ReviewNote }) });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("This report changed. Refresh before reviewing it."); }
        return Ok(new { report.SaleReportId, report.Status });
    }
}
