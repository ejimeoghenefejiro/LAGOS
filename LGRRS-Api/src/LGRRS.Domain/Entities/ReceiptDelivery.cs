using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

public class ReceiptDelivery
{
    public Guid DeliveryId { get; set; } = Guid.NewGuid();
    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public DeliveryChannel Channel { get; set; }
    public string DestinationMasked { get; set; } = string.Empty;
    public string? ProviderRef { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.ReadyToSend;
    public DateTimeOffset? SentAt { get; set; }
}
