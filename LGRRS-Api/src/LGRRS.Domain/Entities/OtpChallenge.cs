using System.ComponentModel.DataAnnotations;
namespace LGRRS.Domain.Entities;
public class OtpChallenge
{
    public Guid OtpChallengeId { get; set; } = Guid.NewGuid();
    public Guid? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }
    [MaxLength(64)] public string PhoneHash { get; set; } = "";
    [MaxLength(20)] public string Purpose { get; set; } = "";
    [MaxLength(6)] public string Code { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int FailedAttempts { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
