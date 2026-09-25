using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

public class RewardEntry
{
    public Guid EntryId { get; set; } = Guid.NewGuid();
    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public Guid DrawPeriodId { get; set; }
    public DrawPeriod? DrawPeriod { get; set; }

    public EligibilityStatus EligibilityStatus { get; set; } = EligibilityStatus.Pending;
    public RiskStatus RiskStatus { get; set; } = RiskStatus.Clear;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DrawResult? DrawResult { get; set; }
}
