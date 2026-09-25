using LGRRS.Domain.Entities;
using LGRRS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace LGRRS.Api.Auth;

public static class AuditActorDisplay
{
    public static async Task ResolveLegacy(LgrrsDbContext db, List<AuditEvent> events)
    {
        var ids = events.Where(e => e.ActorName == null).Select(e => Guid.TryParse(e.ActorId, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
        var staff = await db.AppUsers.AsNoTracking().Where(u => ids.Contains(u.UserId)).ToDictionaryAsync(u => u.UserId);
        var merchants = await db.Merchants.AsNoTracking().Where(m => ids.Contains(m.MerchantId)).ToDictionaryAsync(m => m.MerchantId);
        foreach (var e in events.Where(e => e.ActorName == null))
        {
            if (Guid.TryParse(e.ActorId, out var id) && staff.TryGetValue(id, out var user))
            { e.ActorName = user.DisplayName; e.ActorRole = user.Role.ToString(); }
            else if (Guid.TryParse(e.ActorId, out id) && merchants.TryGetValue(id, out var merchant))
            { e.ActorName = merchant.BusinessName; e.ActorRole = "Merchant"; }
            else if (e.EventType is "SaleReportSubmitted" or "PaperReceiptClaimed" && !string.IsNullOrEmpty(e.ActorId))
            { e.ActorName = "Verified customer (legacy record)"; e.ActorRole = "Consumer"; }
            else { e.ActorName = "Not recorded (legacy event)"; e.ActorRole = "Unknown"; }
        }
    }
}
