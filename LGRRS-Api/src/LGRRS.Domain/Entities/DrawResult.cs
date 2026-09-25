namespace LGRRS.Domain.Entities;

public class DrawResult
{
    public Guid DrawResultId { get; set; } = Guid.NewGuid();
    public Guid DrawPeriodId { get; set; }
    public DrawPeriod? DrawPeriod { get; set; }

    public Guid EntryId { get; set; }
    public RewardEntry? Entry { get; set; }

    public string PrizeTier { get; set; } = string.Empty;
    public decimal PrizeAmount { get; set; }
    public DateTimeOffset DrawTimestamp { get; set; } = DateTimeOffset.UtcNow;

    public PrizeClaim? PrizeClaim { get; set; }
}
