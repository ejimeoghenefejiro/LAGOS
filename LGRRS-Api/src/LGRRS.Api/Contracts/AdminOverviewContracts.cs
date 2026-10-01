namespace LGRRS.Api.Contracts;

public record KpiMetric(decimal Value, decimal? WeekOverWeekChangePercent);

public record WeeklyPoint(string WeekLabel, int Count);

public record TopMerchant(Guid MerchantId, string BusinessName, string LgaCode, int ReceiptsIssued);

public record FraudRiskSummary(int High, int Medium, int Low);

public record AdminOverviewResponse(
    bool IsDemoData,
    KpiMetric VerifiedReceipts,
    KpiMetric ParticipatingMerchants,
    KpiMetric UniqueResidents,
    KpiMetric TotalTransactionValue,
    KpiMetric RewardEntriesThisWeek,
    KpiMetric FraudAttemptsBlocked,
    KpiMetric PrizesWonThisWeek,
    decimal CurrentPrizePool,
    int? DrawClosesInDays,
    List<WeeklyPoint> ReceiptTrend,
    List<TopMerchant> TopMerchants,
    FraudRiskSummary FraudRisk);

public record CurrentDrawResponse(
    Guid DrawPeriodId,
    string Type,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    DateTimeOffset? DrawDate,
    string Status,
    decimal PrizeBudget,
    int EligibleEntries, string? LgaCode);

public record AuditEventItem(
    Guid EventId,
    string EventType,
    string EntityType,
    Guid? EntityId,
    DateTimeOffset Timestamp,
    string Summary,
    string StatusLabel,
    string StatusTone,
    string? ActorName = null,
    string? ActorRole = null);

public record DrawWinnerItem(
    Guid DrawResultId,
    string PrizeTier,
    decimal PrizeAmount,
    string ReceiptRefMasked,
    string MerchantName,
    string LgaCode,
    DateTimeOffset DrawTimestamp,
    bool HasClaim);

public record RunDrawResponse(
    Guid DrawPeriodId,
    int CandidateSetCount,
    string CandidateSetHash,
    List<DrawWinnerItem> Winners);
