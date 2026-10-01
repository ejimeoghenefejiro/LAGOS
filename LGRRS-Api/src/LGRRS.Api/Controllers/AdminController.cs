using System.Security.Cryptography;
using System.Text;
using LGRRS.Api.Auth;
using LGRRS.Api.Contracts;
using LGRRS.Domain;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.LgaAdmin)]
public class AdminController : ControllerBase
{
    private readonly LgrrsDbContext _db;

    public AdminController(LgrrsDbContext db)
    {
        _db = db;
    }

    private static decimal? PercentChange(int thisPeriod, int lastPeriod)
    {
        if (lastPeriod == 0)
        {
            return thisPeriod == 0 ? 0 : null;
        }

        return Math.Round((thisPeriod - lastPeriod) / (decimal)lastPeriod * 100, 1);
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewResponse>> Overview()
    {
        var now = DateTimeOffset.UtcNow;
        var weekAgo = now.AddDays(-7);
        var twoWeeksAgo = now.AddDays(-14);

        var verifiedReceiptsTotal = await _db.Receipts.CountAsync(r => r.Status == ReceiptStatus.Valid);
        var receiptsThisWeek = await _db.Receipts.CountAsync(r => r.Status == ReceiptStatus.Valid && r.TransactionDate >= weekAgo);
        var receiptsLastWeek = await _db.Receipts.CountAsync(r => r.Status == ReceiptStatus.Valid && r.TransactionDate >= twoWeeksAgo && r.TransactionDate < weekAgo);

        var merchantsTotal = await _db.Merchants.CountAsync(m => m.Status == MerchantStatus.Verified);
        var merchantsThisWeek = await _db.Merchants.CountAsync(m => m.Status == MerchantStatus.Verified && m.VerifiedAt >= weekAgo);
        var merchantsLastWeek = await _db.Merchants.CountAsync(m => m.Status == MerchantStatus.Verified && m.VerifiedAt >= twoWeeksAgo && m.VerifiedAt < weekAgo);

        var residentsTotal = await _db.Receipts.Where(r => r.CustomerPhoneHash != null).Select(r => r.CustomerPhoneHash).Distinct().CountAsync();
        var residentsThisWeek = await _db.Receipts.Where(r => r.CustomerPhoneHash != null && r.TransactionDate >= weekAgo).Select(r => r.CustomerPhoneHash).Distinct().CountAsync();
        var residentsLastWeek = await _db.Receipts.Where(r => r.CustomerPhoneHash != null && r.TransactionDate >= twoWeeksAgo && r.TransactionDate < weekAgo).Select(r => r.CustomerPhoneHash).Distinct().CountAsync();

        var totalValue = await _db.Receipts.Where(r => r.Status == ReceiptStatus.Valid).SumAsync(r => (decimal?)r.Amount) ?? 0m;
        var valueThisWeek = await _db.Receipts.Where(r => r.Status == ReceiptStatus.Valid && r.TransactionDate >= weekAgo).SumAsync(r => (decimal?)r.Amount) ?? 0m;
        var valueLastWeek = await _db.Receipts.Where(r => r.Status == ReceiptStatus.Valid && r.TransactionDate >= twoWeeksAgo && r.TransactionDate < weekAgo).SumAsync(r => (decimal?)r.Amount) ?? 0m;
        var valueDelta = valueLastWeek == 0 ? (decimal?)null : Math.Round((valueThisWeek - valueLastWeek) / valueLastWeek * 100, 1);

        var entriesThisWeek = await _db.RewardEntries.CountAsync(e => e.EligibilityStatus == EligibilityStatus.Eligible && e.CreatedAt >= weekAgo);
        var entriesLastWeek = await _db.RewardEntries.CountAsync(e => e.EligibilityStatus == EligibilityStatus.Eligible && e.CreatedAt >= twoWeeksAgo && e.CreatedAt < weekAgo);

        var fraudTotal = await _db.FraudFlags.CountAsync();
        var fraudThisWeek = await _db.FraudFlags.CountAsync(f => f.CreatedAt >= weekAgo);
        var fraudLastWeek = await _db.FraudFlags.CountAsync(f => f.CreatedAt >= twoWeeksAgo && f.CreatedAt < weekAgo);

        var prizesThisWeek = await _db.DrawResults.CountAsync(r => r.DrawTimestamp >= weekAgo);
        var prizesLastWeek = await _db.DrawResults.CountAsync(r => r.DrawTimestamp >= twoWeeksAgo && r.DrawTimestamp < weekAgo);

        var currentDraw = await _db.DrawPeriods
            .Where(d => d.Status == DrawPeriodStatus.Open)
            .OrderByDescending(d => d.StartDate)
            .FirstOrDefaultAsync();

        int? drawClosesInDays = currentDraw is null ? null : Math.Max(0, (int)Math.Ceiling((currentDraw.EndDate - now).TotalDays));

        var trend = new List<WeeklyPoint>();
        for (var i = 5; i >= 0; i--)
        {
            var weekStart = now.AddDays(-7 * (i + 1));
            var weekEnd = now.AddDays(-7 * i);
            var count = await _db.Receipts.CountAsync(r => r.Status == ReceiptStatus.Valid && r.TransactionDate >= weekStart && r.TransactionDate < weekEnd);
            trend.Add(new WeeklyPoint($"{weekStart:MMM d}", count));
        }

        var topMerchants = await _db.Receipts
            .Where(r => r.Status == ReceiptStatus.Valid)
            .GroupBy(r => r.MerchantId)
            .Select(g => new { MerchantId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();

        var merchantLookup = await _db.Merchants
            .Where(m => topMerchants.Select(t => t.MerchantId).Contains(m.MerchantId))
            .ToDictionaryAsync(m => m.MerchantId);

        var topMerchantItems = topMerchants
            .Select(t => merchantLookup.TryGetValue(t.MerchantId, out var m)
                ? new TopMerchant(t.MerchantId, m.BusinessName, m.LgaCode, t.Count)
                : new TopMerchant(t.MerchantId, "Unknown", "-", t.Count))
            .ToList();

        var fraudHigh = await _db.FraudFlags.CountAsync(f => f.Severity == FraudSeverity.High);
        var fraudMedium = await _db.FraudFlags.CountAsync(f => f.Severity == FraudSeverity.Medium);
        var fraudLow = await _db.FraudFlags.CountAsync(f => f.Severity == FraudSeverity.Low);

        return Ok(new AdminOverviewResponse(
            true,
            new KpiMetric(verifiedReceiptsTotal, PercentChange(receiptsThisWeek, receiptsLastWeek)),
            new KpiMetric(merchantsTotal, PercentChange(merchantsThisWeek, merchantsLastWeek)),
            new KpiMetric(residentsTotal, PercentChange(residentsThisWeek, residentsLastWeek)),
            new KpiMetric(totalValue, valueDelta),
            new KpiMetric(entriesThisWeek, PercentChange(entriesThisWeek, entriesLastWeek)),
            new KpiMetric(fraudTotal, PercentChange(fraudThisWeek, fraudLastWeek)),
            new KpiMetric(prizesThisWeek, PercentChange(prizesThisWeek, prizesLastWeek)),
            currentDraw?.PrizeBudget ?? 0,
            drawClosesInDays,
            trend,
            topMerchantItems,
            new FraudRiskSummary(fraudHigh, fraudMedium, fraudLow)));
    }

    public record CreateDrawRequest(string Type, decimal PrizeBudget, int WinnerCount = 50);

    [HttpGet("draws")]
    public async Task<IActionResult> DrawHistory()
    {
        var draws = await _db.DrawPeriods.AsNoTracking().OrderByDescending(d => d.StartDate).Take(100)
            .Select(d => new CurrentDrawResponse(d.DrawPeriodId, d.Type.ToString(),
                d.StartDate, d.EndDate, d.DrawDate, d.Status.ToString(), d.PrizeBudget,
                d.RewardEntries.Count(e => e.EligibilityStatus == EligibilityStatus.Eligible &&
                    e.RiskStatus == RiskStatus.Clear && e.Receipt!.Status == ReceiptStatus.Valid &&
                    e.Receipt.Merchant!.Status == MerchantStatus.Verified))).ToListAsync();
        return Ok(draws);
    }

    [HttpPost("draws")]
    public async Task<IActionResult> CreateDraw(CreateDrawRequest request)
    {
        if (request.Type is not ("Weekly" or "Monthly"))
            return BadRequest("Choose Weekly or Monthly.");
        if (request.WinnerCount < 1 || request.WinnerCount > 1000) return BadRequest("Choose between 1 and 1000 winners.");
        if (request.PrizeBudget * 100 % request.WinnerCount != 0) return BadRequest("Budget must divide equally between winners to the nearest kobo.");
        var minimumBudget = request.WinnerCount * 0.01m;
        if (request.PrizeBudget < minimumBudget || request.PrizeBudget > 1000000000m ||
            decimal.Round(request.PrizeBudget, 2) != request.PrizeBudget)
            return BadRequest($"Prize budget must be between {minimumBudget} and 1000000000, with at most two decimal places.");
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await LockDrawAdministration();
        if (await _db.DrawPeriods.AnyAsync(d => d.Status == DrawPeriodStatus.Open))
            return Conflict("An open draw already exists. Complete it before creating another.");
        var start = DateTimeOffset.UtcNow;
        var draw = new DrawPeriod {
            Type = request.Type == "Weekly" ? DrawPeriodType.Weekly : DrawPeriodType.Monthly,
            StartDate = start, EndDate = request.Type == "Weekly" ? start.AddDays(7) : start.AddMonths(1),
            PrizeBudget = request.PrizeBudget, WinnerCount = request.WinnerCount
        };
        _db.DrawPeriods.Add(draw);
        _db.AuditEvents.Add(new AuditEvent { EventType = "DrawCreated", EntityType = "DrawPeriod",
            EntityId = draw.DrawPeriodId, ActorId = User.Identity?.Name,
            Metadata = $"{request.Type} draw; budget {request.PrizeBudget}" });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new CurrentDrawResponse(draw.DrawPeriodId, draw.Type.ToString(), draw.StartDate,
            draw.EndDate, null, "Open", draw.PrizeBudget, 0));
    }

    private Task<int> LockDrawAdministration() => _db.Database.ExecuteSqlRawAsync(
        "DECLARE @result int; EXEC @result = sp_getapplock @Resource = 'LGRRS.DrawAdministration', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Draw administration is busy. Try again.', 1;");

    [HttpGet("draws/current")]
    public async Task<ActionResult<CurrentDrawResponse>> CurrentDraw()
    {
        var draw = await _db.DrawPeriods
            .Where(d => d.Status == DrawPeriodStatus.Open)
            .OrderByDescending(d => d.StartDate)
            .FirstOrDefaultAsync();

        if (draw is null)
        {
            return NotFound();
        }

        var eligibleEntries = await _db.RewardEntries.CountAsync(e => e.DrawPeriodId == draw.DrawPeriodId &&
            e.EligibilityStatus == EligibilityStatus.Eligible && e.RiskStatus == RiskStatus.Clear &&
            e.Receipt!.Status == ReceiptStatus.Valid && e.Receipt.Merchant!.Status == MerchantStatus.Verified);

        return Ok(new CurrentDrawResponse(
            draw.DrawPeriodId,
            draw.Type.ToString(),
            draw.StartDate,
            draw.EndDate,
            draw.DrawDate,
            draw.Status.ToString(),
            draw.PrizeBudget,
            eligibleEntries));
    }


    public record LocationPreview(int WinnerCount, decimal PrizePerWinner, bool CanRun, string PreviewToken, List<LocationDraw.Allocation> Locations);
    private async Task<LocationPreview> BuildLocationPreview(DrawPeriod draw, List<RewardEntry> candidates)
    {
        // Include locations with entries that are all blocked: show their zero eligible count.
        var locations = await _db.RewardEntries.Where(e => e.DrawPeriodId == draw.DrawPeriodId)
            .Select(e => e.Receipt!.Merchant!.LgaCode).Distinct().ToListAsync();
        var customers = LocationDraw.Customers(candidates);
        var allocations = LocationDraw.Allocate(draw.DrawPeriodId, draw.WinnerCount, locations, customers);
        var payload = System.Text.Json.JsonSerializer.Serialize(new { draw.DrawPeriodId, draw.PrizeBudget, draw.WinnerCount,
            Allocations = allocations, Customers = customers.Select(c => new { c.EntryId, Location = LocationDraw.Location(c), c.Receipt!.CustomerPhoneHash }) });
        var token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var valid = draw.WinnerCount > 0 && draw.PrizeBudget > 0 && draw.PrizeBudget * 100 % Math.Max(1, draw.WinnerCount) == 0;
        return new(draw.WinnerCount, valid ? draw.PrizeBudget / draw.WinnerCount : 0,
            valid && allocations.Count > 0 && allocations.All(a => a.Shortfall == 0), token, allocations);
    }
    [HttpGet("draws/{drawPeriodId:guid}/preview")]
    public async Task<IActionResult> PreviewDraw(Guid drawPeriodId)
    {
        var draw = await _db.DrawPeriods.FindAsync(drawPeriodId);
        if (draw == null) return NotFound();
        if (draw.Status != DrawPeriodStatus.Open) return Conflict("This draw is already published.");
        var candidates = await _db.RewardEntries.Include(e => e.Receipt).ThenInclude(r => r!.Merchant)
            .Where(e => e.DrawPeriodId == drawPeriodId && e.EligibilityStatus == EligibilityStatus.Eligible && e.RiskStatus == RiskStatus.Clear
                && e.Receipt!.Status == ReceiptStatus.Valid && e.Receipt.Merchant!.Status == MerchantStatus.Verified && e.DrawResult == null).ToListAsync();
        return Ok(await BuildLocationPreview(draw, candidates));
    }
    public record RunLocationDrawRequest(string PreviewToken);
    [HttpPost("draws/{drawPeriodId:guid}/run")]
    public async Task<ActionResult<RunDrawResponse>> RunDraw(Guid drawPeriodId, RunLocationDrawRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await LockDrawAdministration();
        var draw = await _db.DrawPeriods.FirstOrDefaultAsync(d => d.DrawPeriodId == drawPeriodId);
        if (draw is null)
        {
            return NotFound();
        }

        if (draw.Status != DrawPeriodStatus.Open)
        {
            return Conflict("This draw has already been run.");
        }

        // Freeze the candidate population before selecting winners (spec 13.3: candidate set is
        // frozen before the draw and its size/hash are persisted for auditability).
        var candidates = await _db.RewardEntries
            .Include(e => e.Receipt)
            .ThenInclude(r => r!.Merchant)
            .Where(e => e.DrawPeriodId == drawPeriodId
                && e.EligibilityStatus == EligibilityStatus.Eligible
                && e.RiskStatus == RiskStatus.Clear
                && e.Receipt!.Status == ReceiptStatus.Valid
                && e.Receipt.Merchant!.Status == MerchantStatus.Verified
                && e.DrawResult == null)
            .ToListAsync();

        if (candidates.Count == 0) return Conflict("No eligible entries yet. Issue receipts during this draw period first.");
        var preview = await BuildLocationPreview(draw, candidates);
        if (request.PreviewToken != preview.PreviewToken) return Conflict("Eligible customers changed. Refresh the location preview before publishing.");
        if (!preview.CanRun) return Conflict("Some locations have too few eligible customers. No winners were selected; refresh the preview.");
        draw.CandidateSetCount = candidates.Count;
        draw.CandidateSetHash = ComputeCandidateHash(candidates.Select(c => c.EntryId));
        var createdResults = new List<DrawResult>();
        foreach (var picked in LocationDraw.Pick(preview.Locations, LocationDraw.Customers(candidates)))
        {
            var result = new DrawResult { DrawPeriodId = drawPeriodId, EntryId = picked.EntryId,
                PrizeTier = "Equal", PrizeAmount = preview.PrizePerWinner, DrawTimestamp = DateTimeOffset.UtcNow };
            _db.DrawResults.Add(result); createdResults.Add(result);
        }
        _db.AuditEvents.Add(new AuditEvent { EventType = "DrawLocationAllocation", EntityType = "DrawPeriod", EntityId = drawPeriodId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(preview) });
        draw.Status = DrawPeriodStatus.Published;
        draw.DrawDate = DateTimeOffset.UtcNow;

        _db.AuditEvents.Add(new AuditEvent
        {
            EventType = "DrawCompleted",
            EntityType = "DrawPeriod",
            EntityId = draw.DrawPeriodId,
            Metadata = $"{draw.Type} draw ({createdResults.Count} winners)"
        });

        await _db.SaveChangesAsync();

        var winners = createdResults.Select(r =>
        {
            var entry = candidates.First(c => c.EntryId == r.EntryId);
            return new DrawWinnerItem(
                r.DrawResultId,
                r.PrizeTier,
                r.PrizeAmount,
                entry.Receipt!.ReceiptRefMasked,
                entry.Receipt.Merchant?.BusinessName ?? "Unknown",
                entry.Receipt.Merchant?.LgaCode ?? "-",
                r.DrawTimestamp,
                false);
        }).ToList();

        await transaction.CommitAsync();
        return Ok(new RunDrawResponse(draw.DrawPeriodId, draw.CandidateSetCount ?? 0, draw.CandidateSetHash ?? "", winners));
    }

    [HttpGet("draws/{drawPeriodId:guid}/winners")]
    public async Task<ActionResult<List<DrawWinnerItem>>> DrawWinners(Guid drawPeriodId)
    {
        var results = await _db.DrawResults
            .Include(r => r.Entry)
            .ThenInclude(e => e!.Receipt)
            .ThenInclude(r => r!.Merchant)
            .Include(r => r.PrizeClaim)
            .Where(r => r.DrawPeriodId == drawPeriodId)
            .OrderByDescending(r => r.DrawTimestamp)
            .ToListAsync();

        var winners = results.Select(r => new DrawWinnerItem(
            r.DrawResultId,
            r.PrizeTier,
            r.PrizeAmount,
            r.Entry?.Receipt?.ReceiptRefMasked ?? "-",
            r.Entry?.Receipt?.Merchant?.BusinessName ?? "Unknown",
            r.Entry?.Receipt?.Merchant?.LgaCode ?? "-",
            r.DrawTimestamp,
            r.PrizeClaim != null)).ToList();

        return Ok(winners);
    }

    private static string ComputeCandidateHash(IEnumerable<Guid> entryIds)
    {
        var joined = string.Join(",", entryIds.OrderBy(id => id));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
        return Convert.ToHexString(hash);
    }

    [HttpGet("audit-events")]
    public async Task<ActionResult<List<AuditEventItem>>> AuditEvents([FromQuery] int take = 10)
    {
        var events = await _db.AuditEvents.AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();

        await AuditActorDisplay.ResolveLegacy(_db, events);
        var items = events.Select(e =>
        {
            var (summary, statusLabel, statusTone) = DescribeEvent(e.EventType, e.Metadata);
            return new AuditEventItem(e.EventId, e.EventType, e.EntityType, e.EntityId, e.Timestamp, summary, statusLabel, statusTone, e.ActorName, e.ActorRole);
        }).ToList();

        return Ok(items);
    }

    private static (string Summary, string StatusLabel, string StatusTone) DescribeEvent(string eventType, string? metadata) => eventType switch
    {
        "ReceiptIssued" => ($"Receipt {metadata} was issued", "Issued", "blue"),
        "RewardEntryCreated" => ($"New reward entry {metadata} was created", "Entry Created", "blue"),
        "FraudFlagRaised" => ($"Suspicious activity {metadata} was flagged", "Blocked", "red"),
        "ClaimSubmitted" => ($"Prize claim {metadata} was submitted", "Submitted", "green"),
        "MerchantVerified" => ($"Merchant {metadata} was verified", "Verified", "green"),
        "DrawCompleted" => ($"{metadata} was completed", "Draw Completed", "purple"),
        _ => (eventType, eventType, "gray")
    };

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var verifiedReceipts = await _db.Receipts.CountAsync(r => r.Status == ReceiptStatus.Valid);
        var participatingMerchants = await _db.Merchants.CountAsync(m => m.Status == MerchantStatus.Verified);
        var totalValue = await _db.Receipts.Where(r => r.Status == ReceiptStatus.Valid).SumAsync(r => (decimal?)r.Amount) ?? 0m;
        var rewardEntries = await _db.RewardEntries.CountAsync(e => e.EligibilityStatus == EligibilityStatus.Eligible);
        var fraudBlocked = await _db.FraudFlags.CountAsync(f => f.Status == FraudFlagStatus.Resolved);
        var prizesWon = await _db.DrawResults.CountAsync();
        var currentPrizePool = await _db.DrawPeriods
            .Where(d => d.Status == DrawPeriodStatus.Open)
            .Select(d => d.PrizeBudget)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            IsDemoData = true,
            VerifiedReceipts = verifiedReceipts,
            ParticipatingMerchants = participatingMerchants,
            TotalTransactionValue = totalValue,
            RewardEntries = rewardEntries,
            FraudAttemptsBlocked = fraudBlocked,
            PrizesWon = prizesWon,
            CurrentPrizePool = currentPrizePool
        });
    }

    [HttpGet("merchants")]
    public async Task<IActionResult> Merchants()
    {
        var merchants = await _db.Merchants
            .Select(m => new
            {
                m.MerchantId,
                m.BusinessName,
                m.BusinessType,
                m.LgaCode,
                Status = m.Status.ToString()
            })
            .ToListAsync();

        return Ok(merchants);
    }

    [HttpGet("merchants/sales")]
    public async Task<IActionResult> MerchantSales()
    {
        var sales = await _db.Receipts
            .Where(r => r.Status == ReceiptStatus.Valid)
            .GroupBy(r => r.MerchantId)
            .Select(g => new
            {
                MerchantId = g.Key,
                ReceiptsIssued = g.Count(),
                SalesValue = g.Sum(r => r.Amount)
            })
            .ToListAsync();

        var merchants = await _db.Merchants.ToDictionaryAsync(m => m.MerchantId);

        var result = sales.Select(s => new
        {
            s.MerchantId,
            BusinessName = merchants.TryGetValue(s.MerchantId, out var m) ? m.BusinessName : "Unknown",
            BusinessType = merchants.TryGetValue(s.MerchantId, out var m2) ? m2.BusinessType : "Unknown",
            s.ReceiptsIssued,
            s.SalesValue
        });

        return Ok(result);
    }

    [HttpGet("fraud-flags")]
    public async Task<IActionResult> FraudFlags()
    {
        var flags = await _db.FraudFlags
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.FlagId,
                EntityType = f.EntityType.ToString(),
                f.EntityId,
                RuleCode = f.RuleCode.ToString(),
                Severity = f.Severity.ToString(),
                Status = f.Status.ToString(),
                f.CreatedAt
            })
            .ToListAsync();

        return Ok(flags);
    }

    [HttpGet("merchants/{merchantId:guid}")]
    public async Task<IActionResult> MerchantDetail(Guid merchantId, [FromServices] LGRRS.Infrastructure.Security.IDataProtectionCodec codec)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant is null)
        {
            return NotFound();
        }

        var receiptsQuery = _db.Receipts.Where(r => r.MerchantId == merchantId && r.Status == ReceiptStatus.Valid);
        var receiptsIssued = await receiptsQuery.CountAsync();
        var salesValue = await receiptsQuery.SumAsync(r => (decimal?)r.Amount) ?? 0m;
        var rewardEntries = await _db.RewardEntries.CountAsync(e => e.Receipt!.MerchantId == merchantId && e.EligibilityStatus == EligibilityStatus.Eligible);

        return Ok(new
        {
            merchant.MerchantId,
            merchant.BusinessName,
            merchant.BusinessType,
            merchant.LgaCode,
            merchant.LgrrsSystemId,
            Status = merchant.Status.ToString(),
            merchant.CreatedAt,
            merchant.VerifiedAt,
            merchant.BusinessAddress,
            PhoneNumber = codec.Decrypt(merchant.PhoneEncrypted),
            merchant.ReviewReason, merchant.ReviewedAt,
            ReceiptsIssued = receiptsIssued,
            SalesValue = salesValue,
            AverageSale = receiptsIssued > 0 ? Math.Round(salesValue / receiptsIssued, 2) : 0,
            RewardEntries = rewardEntries
        });
    }

    [HttpGet("merchants/{merchantId:guid}/receipts")]
    public async Task<IActionResult> MerchantReceipts(Guid merchantId)
    {
        var receipts = await _db.Receipts
            .Where(r => r.MerchantId == merchantId)
            .Include(r => r.Deliveries)
            .OrderByDescending(r => r.TransactionDate)
            .Take(100)
            .ToListAsync();

        var result = receipts.Select(r => new
        {
            ReceiptRefMasked = r.ReceiptRefMasked,
            r.CustomerName,
            r.CustomerPhoneMasked,
            r.ItemService,
            r.Amount,
            Status = r.Status.ToString(),
            DeliveryStatus = r.Deliveries.OrderByDescending(d => d.SentAt).FirstOrDefault()?.Status.ToString() ?? "RECORDED",
            r.TransactionDate
        });

        return Ok(result);
    }

    [HttpGet("receipts")]
    public async Task<IActionResult> Receipts([FromQuery] string? status, [FromQuery] string? search, [FromQuery] int take = 100)
    {
        var query = _db.Receipts.Include(r => r.Merchant).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReceiptStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Merchant!.BusinessName.Contains(search) || r.ItemService.Contains(search));
        }

        var receipts = await query
            .OrderByDescending(r => r.TransactionDate)
            .Take(Math.Clamp(take, 1, 500))
            .Select(r => new
            {
                ReceiptRefMasked = r.ReceiptRefMasked,
                MerchantName = r.Merchant!.BusinessName,
                LgaCode = r.Merchant!.LgaCode,
                r.CustomerName,
                r.CustomerPhoneMasked,
                r.ItemService,
                r.Amount,
                Status = r.Status.ToString(),
                r.TransactionDate
            })
            .ToListAsync();

        return Ok(receipts);
    }
}
