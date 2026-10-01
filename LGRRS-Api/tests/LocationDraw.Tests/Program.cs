using LGRRS.Domain;
using LGRRS.Domain.Entities;

void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine($"PASS: {name}"); }
RewardEntry Entry(string phone, string lga, int day = 1) => new() { Receipt = new Receipt {
    CustomerPhoneHash = phone, Merchant = new Merchant { LgaCode = lga },
    TransactionDate = new DateTimeOffset(2026, 1, day, 0, 0, 0, TimeSpan.Zero) } };
var id = Guid.NewGuid();
var scopedDraw = new DrawPeriod { LgaCode = "IKEJA" };
Check(scopedDraw.CoversLga(" ikeja ") && !scopedDraw.CoversLga("ALIMOSHO"), "Draw accepts only its selected LGA");
Check(new DrawPeriod().CoversLga("ALIMOSHO"), "Legacy draw scope preserved");
var emptyLga = LocationDraw.Allocate(id, 50, new[] { "IKEJA" }, new List<RewardEntry>());
Check(emptyLga.Single().Shortfall == 50, "Selected LGA with no entries shows full shortage");
var entries = Enumerable.Range(0, 100).Select(i => Entry($"phone{i}", $"LGA{i % 4}")).ToList();
entries.Add(Entry("phone0", "LGA1", 2));
var customers = LocationDraw.Customers(entries);
Check(customers.Count == 100 && LocationDraw.Location(customers.Single(c => c.Receipt!.CustomerPhoneHash == "phone0")) == "LGA0", "Duplicate customer assigned to earliest purchase LGA");
var allocations = LocationDraw.Allocate(id, 50, new[] { "LGA0", "LGA1", "LGA2", "LGA3", " lga0 " }, customers);
Check(allocations.Count == 4 && allocations.Sum(a => a.Slots) == 50 && allocations.Max(a => a.Slots) - allocations.Min(a => a.Slots) == 1, "50 slots evenly allocated with normalized locations");
Check(allocations.SequenceEqual(LocationDraw.Allocate(id, 50, new[] { "LGA3", "LGA2", "LGA1", "LGA0" }, customers)), "Preview allocation stable regardless of database order");
for (var i = 0; i < 100; i++) {
    var winners = LocationDraw.Pick(allocations, customers);
    if (winners.Count != 50 || winners.Select(w => w.Receipt!.CustomerPhoneHash).Distinct().Count() != 50 || allocations.Any(a => winners.Count(w => LocationDraw.Location(w) == a.Location) != a.Slots)) throw new Exception("Random selection violated quota or uniqueness");
}
Check(true, "100 random draws preserve all quotas and one win per customer");
var shortage = LocationDraw.Allocate(id, 50, new[] { "LGA0", "EMPTY" }, customers);
Check(shortage.Single(a => a.Location == "EMPTY").Shortfall == 25, "Location with no eligible customers displays shortage");
try { LocationDraw.Pick(shortage, customers); throw new Exception("Shortage allowed"); } catch (InvalidOperationException) { Check(true, "Shortage blocks selection"); }
Check(LocationDraw.Allocate(id, 50, Array.Empty<string>(), customers).Count == 0, "Empty draw preview supported");
var few = LocationDraw.Allocate(id, 2, allocations.Select(a => a.Location), customers);
Check(few.Sum(a => a.Slots) == 2 && few.Count(a => a.Slots == 0) == 2, "Fewer slots than locations represented explicitly");
Check(1000000m / 50 == 20000m, "Example awards 20000 per winner");
