using System.ComponentModel.DataAnnotations;
namespace LGRRS.Domain.Entities;
public class CatalogItem
{
    public Guid CatalogItemId { get; set; } = Guid.NewGuid();
    public Guid MerchantId { get; set; }
    public Merchant? Merchant { get; set; }
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(10)] public string Kind { get; set; } = "Service";
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
