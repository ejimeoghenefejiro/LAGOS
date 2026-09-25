using System.Security.Claims;
using LGRRS.Api.Auth;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController]
[Route("api/consumer/receipts")]
[Authorize(Roles = Roles.Consumer)]
public class ConsumerReceiptsController(LgrrsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Wallet()
    {
        var phone = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var receipts = await db.Receipts.AsNoTracking()
            .Where(r => r.CustomerPhoneHash == phone)
            .Include(r => r.Merchant)
            .Include(r => r.RewardEntry).ThenInclude(e => e!.DrawPeriod)
            .Include(r => r.RewardEntry).ThenInclude(e => e!.DrawResult)
            .OrderByDescending(r => r.TransactionDate).Take(100).ToListAsync();
        return Ok(receipts.Select(r => new {
            r.ReceiptId, MerchantName = r.Merchant!.BusinessName, r.ItemService,
            r.Amount, r.TransactionDate,
            Status = r.Status.ToString(),
            EntryStatus = r.RewardEntry == null ? "No entry"
                : r.Status != ReceiptStatus.Valid || r.RewardEntry.EligibilityStatus != EligibilityStatus.Eligible ? "Not eligible"
                : r.RewardEntry.RiskStatus != RiskStatus.Clear ? "Under review"
                : r.RewardEntry.DrawResult != null ? "Winner"
                : r.RewardEntry.DrawPeriod!.Status == DrawPeriodStatus.Published ? "Not selected" : "Draw pending"
        }));
    }
}
