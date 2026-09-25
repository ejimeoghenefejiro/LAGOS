using System.Security.Claims;
using LGRRS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LGRRS.Api.Auth;

public class AuditActorInterceptor(IHttpContextAccessor accessor) : SaveChangesInterceptor
{
    private void Stamp(DbContext? db)
    {
        if (db == null) return;
        var context = accessor.HttpContext;
        var user = context?.User;
        foreach (var entry in db.ChangeTracker.Entries<AuditEvent>().Where(e => e.State == EntityState.Added))
        {
            var audit = entry.Entity;
            if (context?.Items["PosAuditMerchant"] is Merchant merchant)
            {
                audit.ActorId = merchant.MerchantId.ToString();
                audit.ActorName = merchant.BusinessName;
                audit.ActorRole = "POS integration";
            }
            else if (user?.Identity?.IsAuthenticated == true)
            {
                audit.ActorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                audit.ActorName = user.FindFirstValue(ClaimTypes.Name) ?? "Authenticated user";
                audit.ActorRole = user.FindFirstValue(ClaimTypes.Role);
            }
            else
            {
                audit.ActorName ??= context == null ? "System" : "Unauthenticated request";
                audit.ActorRole ??= context == null ? "System" : "Unauthenticated";
            }
        }
    }
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    { Stamp(eventData.Context); return result; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Stamp(eventData.Context); return ValueTask.FromResult(result); }
}
