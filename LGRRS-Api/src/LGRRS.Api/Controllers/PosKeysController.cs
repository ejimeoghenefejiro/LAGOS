using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LGRRS.Api.Auth;
using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace LGRRS.Api.Controllers;

[ApiController, Route("api/merchant/pos-key"), Authorize(Roles = Roles.Merchant)]
public class PosKeysController(LgrrsDbContext db) : ControllerBase
{
    private Guid MerchantId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var merchant = await db.Merchants.AsNoTracking().SingleOrDefaultAsync(m => m.MerchantId == MerchantId);
        if (merchant == null) return NotFound();
        return Ok(new { HasKey = merchant.PosApiKeyHash != null,
            Prefix = merchant.PosApiKeyPrefix, ExpiresAt = merchant.PosApiKeyExpiresAt,
            Enabled = merchant.PosIntegrationEnabled });
    }
    [HttpPost]
    public async Task<IActionResult> Generate()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await PosLock.Acquire(db, MerchantId);
        var merchant = await db.Merchants.FindAsync(MerchantId);
        if (merchant == null) return NotFound();
        if (!merchant.PosIntegrationEnabled || merchant.Status != MerchantStatus.Verified)
            return Conflict("Enable POS integration on a verified business before generating a key.");
        var key = "lgrrs_pos_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        merchant.PosApiKeyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        merchant.PosApiKeyPrefix = key[..17];
        merchant.PosApiKeyExpiresAt = DateTimeOffset.UtcNow.AddDays(90);
        db.AuditEvents.Add(new AuditEvent { EventType = "PosKeyGenerated", EntityType = "Merchant",
            EntityId = MerchantId, ActorId = MerchantId.ToString(), Metadata = "Receipt submission key generated; previous key invalidated." });
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        Response.Headers.CacheControl = "no-store";
        return Ok(new { ApiKey = key, Prefix = merchant.PosApiKeyPrefix, ExpiresAt = merchant.PosApiKeyExpiresAt });
    }
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await PosLock.Acquire(db, MerchantId);
        var merchant = await db.Merchants.FindAsync(MerchantId);
        if (merchant == null) return NotFound();
        merchant.PosApiKeyHash = null; merchant.PosApiKeyPrefix = null; merchant.PosApiKeyExpiresAt = null;
        db.AuditEvents.Add(new AuditEvent { EventType = "PosKeyRevoked", EntityType = "Merchant",
            EntityId = MerchantId, ActorId = MerchantId.ToString() });
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return NoContent();
    }
}
internal static class PosLock
{
    public static Task<int> Acquire(LgrrsDbContext db, Guid merchantId)
    {
        var resource = $"LGRRS.POS.{merchantId}";
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"DECLARE @r int; EXEC @r = sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'POS is busy. Retry with the same sale ID.', 1;");
    }
}
