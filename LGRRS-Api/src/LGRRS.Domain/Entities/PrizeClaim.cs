using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

public class PrizeClaim
{
    public Guid ClaimId { get; set; } = Guid.NewGuid();
    public Guid DrawResultId { get; set; }
    public DrawResult? DrawResult { get; set; }

    public string NameEncrypted { get; set; } = string.Empty;
    public string PhoneHash { get; set; } = string.Empty;
    public string BankNameEncrypted { get; set; } = string.Empty;
    public string AccountNumberEncrypted { get; set; } = string.Empty;

    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;
    public string ClaimRef { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
