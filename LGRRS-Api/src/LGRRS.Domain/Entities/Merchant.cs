using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

public class Merchant
{
    public Guid MerchantId { get; set; } = Guid.NewGuid();
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string? BusinessAddress { get; set; }
    public string? ReviewReason { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string LgaCode { get; set; } = string.Empty;

    // Keep the programme ID stable when an official LIRS identifier is linked later.
    public string? LagosTaxIdEncrypted { get; set; }
    public string? LgrrsSystemId { get; set; }
    // Merchant opt-in only; this does not imply an operational POS connection.
    public bool PosIntegrationEnabled { get; set; }
    public string? PosApiKeyHash { get; set; }
    public string? PosApiKeyPrefix { get; set; }
    public DateTimeOffset? PosApiKeyExpiresAt { get; set; }

    public string PhoneEncrypted { get; set; } = string.Empty;
    public string PhoneHash { get; set; } = string.Empty;
    public string? PasscodeHash { get; set; }
    public int PasscodeFailedAttempts { get; set; }
    public DateTimeOffset? PasscodeLockedUntil { get; set; }

    public MerchantStatus Status { get; set; } = MerchantStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? VerifiedAt { get; set; }

    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
