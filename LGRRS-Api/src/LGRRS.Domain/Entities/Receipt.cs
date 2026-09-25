using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

public class Receipt
{
    public Guid ReceiptId { get; set; } = Guid.NewGuid();
    public Guid MerchantId { get; set; }
    public Merchant? Merchant { get; set; }
    public string? PaperClaimHash { get; set; }
    public DateTimeOffset? PaperClaimExpiresAt { get; set; }
    public DateTimeOffset? PaperClaimedAt { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhoneEncrypted { get; set; }
    public string? CustomerPhoneHash { get; set; }
    public string CustomerPhoneMasked { get; set; } = string.Empty;

    public string ItemService { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public ReceiptStatus Status { get; set; } = ReceiptStatus.Valid;
    public Guid? DrawPeriodId { get; set; }
    public DrawPeriod? DrawPeriod { get; set; }

    public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ReceiptDelivery> Deliveries { get; set; } = new List<ReceiptDelivery>();
    public RewardEntry? RewardEntry { get; set; }

    // Masked receipt reference shown to merchants, e.g. ****92K. Never expose ReceiptId itself.
    public string ReceiptRefMasked => $"****{ReceiptId.ToString("N")[^3..].ToUpperInvariant()}";
}
