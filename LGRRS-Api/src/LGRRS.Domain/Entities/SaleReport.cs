using System.ComponentModel.DataAnnotations;

namespace LGRRS.Domain.Entities;

// A customer allegation is not a receipt, taxable turnover, or a reward entry.
public class SaleReport
{
    public Guid SaleReportId { get; set; } = Guid.NewGuid();
    [MaxLength(64)] public string ReporterPhoneHash { get; set; } = "";
    [MaxLength(64)] public string Fingerprint { get; set; } = "";
    [MaxLength(200)] public string BusinessName { get; set; } = "";
    [MaxLength(300)] public string BusinessLocation { get; set; } = "";
    [MaxLength(50)] public string LgaCode { get; set; } = "";
    public DateTimeOffset PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(30)] public string Issue { get; set; } = "";
    [MaxLength(1000)] public string Details { get; set; } = "";
    [MaxLength(30)] public string Status { get; set; } = "Submitted";
    [MaxLength(1000)] public string ReviewNote { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
