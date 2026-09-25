using System.ComponentModel.DataAnnotations;
namespace LGRRS.Domain.Entities;
public class PosSubmission
{
    public Guid PosSubmissionId { get; set; } = Guid.NewGuid();
    public Guid MerchantId { get; set; }
    public Merchant? Merchant { get; set; }
    [MaxLength(100)] public string ExternalSaleId { get; set; } = "";
    [MaxLength(64)] public string PayloadHash { get; set; } = "";
    public string ResponseJson { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
