using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LGRRS.Api.Auth;
using LGRRS.Domain.Entities;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
namespace LGRRS.Api.Controllers;

public record SaveCatalogItem([Required, StringLength(200)] string Name,
    [Required] string Kind, [Range(typeof(decimal), "0.01", "1000000000")] decimal Price, bool IsActive = true);
[ApiController, Route("api/merchant/catalog"), Authorize(Roles = Roles.Merchant)]
public class CatalogController(LgrrsDbContext db) : ControllerBase
{
    private Guid MerchantId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.CatalogItems.AsNoTracking()
        .Where(i => i.MerchantId == MerchantId).OrderBy(i => i.Name)
        .Select(i => new { i.CatalogItemId, i.Name, i.Kind, i.Price, i.IsActive }).ToListAsync());
    [HttpPost]
    public Task<IActionResult> Create(SaveCatalogItem request) => Save(null, request);
    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Update(Guid id, SaveCatalogItem request) => Save(id, request);
    private async Task<IActionResult> Save(Guid? id, SaveCatalogItem request)
    {
        if (request.Kind is not ("Product" or "Service") || decimal.Round(request.Price, 2) != request.Price)
            return BadRequest("Choose Product or Service and enter a price with at most two decimal places.");
        var item = id.HasValue ? await db.CatalogItems.SingleOrDefaultAsync(i => i.CatalogItemId == id && i.MerchantId == MerchantId)
            : new CatalogItem { MerchantId = MerchantId };
        if (item == null) return NotFound();
        item.Name = request.Name.Trim(); item.Kind = request.Kind; item.Price = request.Price; item.IsActive = request.IsActive;
        if (!id.HasValue) db.CatalogItems.Add(item);
        db.AuditEvents.Add(new AuditEvent { EventType = id.HasValue ? "CatalogItemUpdated" : "CatalogItemCreated",
            EntityType = "CatalogItem", EntityId = item.CatalogItemId, ActorId = MerchantId.ToString(), Metadata = item.Name });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { return Conflict("An item with that name already exists. Edit the existing item instead."); }
        return Ok(new { item.CatalogItemId, item.Name, item.Kind, item.Price, item.IsActive });
    }
}
