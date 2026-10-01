using System.Security.Cryptography;
using System.Text;
using LGRRS.Domain.Entities;

namespace LGRRS.Domain;

public static class LocationDraw
{
    public record Allocation(string Location, int Slots, int EligibleCustomers, int Shortfall);
    public static string Location(RewardEntry e) => e.Receipt!.Merchant!.LgaCode.Trim().ToUpperInvariant();
    // A customer belongs to their earliest eligible purchase location in this draw.
    // Additional receipts cannot multiply their chances or let them win in two LGAs.
    public static List<RewardEntry> Customers(IEnumerable<RewardEntry> candidates) => candidates
        .Where(e => !string.IsNullOrWhiteSpace(e.Receipt?.CustomerPhoneHash))
        .OrderBy(e => e.Receipt!.TransactionDate).ThenBy(e => e.EntryId)
        .GroupBy(e => e.Receipt!.CustomerPhoneHash).Select(g => g.First()).ToList();
    public static List<Allocation> Allocate(Guid drawId, int count, IEnumerable<string> locations, List<RewardEntry> customers)
    {
        var ordered = locations.Select(l => l.Trim().ToUpperInvariant()).Distinct()
            .OrderBy(l => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{drawId}:{l}")))).ToList();
        return ordered.Select((l, i) => {
            var slots = count / ordered.Count + (i < count % ordered.Count ? 1 : 0);
            var available = customers.Count(c => Location(c) == l);
            return new Allocation(l, slots, available, Math.Max(0, slots - available));
        }).OrderBy(a => a.Location).ToList();
    }
    public static List<RewardEntry> Pick(List<Allocation> allocations, List<RewardEntry> customers)
    {
        if (allocations.Count == 0 || allocations.Any(a => a.Shortfall > 0)) throw new InvalidOperationException("Insufficient eligible customers by location.");
        var winners = new List<RewardEntry>();
        foreach (var allocation in allocations)
        {
            var pool = customers.Where(c => Location(c) == allocation.Location).ToList();
            for (var i = 0; i < allocation.Slots; i++) {
                var index = RandomNumberGenerator.GetInt32(pool.Count);
                winners.Add(pool[index]); pool.RemoveAt(index);
            }
        }
        return winners;
    }
}
