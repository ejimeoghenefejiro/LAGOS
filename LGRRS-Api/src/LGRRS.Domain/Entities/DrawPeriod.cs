using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

public class DrawPeriod
{
    public Guid DrawPeriodId { get; set; } = Guid.NewGuid();
    public DrawPeriodType Type { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public DateTimeOffset? DrawDate { get; set; }
    public DrawPeriodStatus Status { get; set; } = DrawPeriodStatus.Open;
    public decimal PrizeBudget { get; set; }

    // Persisted for auditability of the frozen candidate population, per spec section 13.3.
    public int? CandidateSetCount { get; set; }
    public string? CandidateSetHash { get; set; }

    public ICollection<RewardEntry> RewardEntries { get; set; } = new List<RewardEntry>();
    public ICollection<DrawResult> DrawResults { get; set; } = new List<DrawResult>();
}
