using LGRRS.Domain.Entities;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using LGRRS.Infrastructure.Security;

namespace LGRRS.Infrastructure.Seed;

// Populates the MVP demo dataset described in spec section 18: one LGA (Ikeja),
// a handful of fictional merchants, synthetic receipts/entries/claims. All figures
// are synthetic and must stay labelled as DEMO DATA in every UI surface.
public static class DemoDataSeeder
{
    private static readonly string[] BusinessTypes =
    {
        "Barbershop", "Salon", "Supermarket", "Restaurant", "Pharmacy", "Fashion", "Electronics"
    };

    private static readonly string[] BusinessNames =
    {
        "Ayo Fadez Barbers", "Ikeja Fresh Cuts", "Bella Hair Studio", "Sunrise Supermarket",
        "Mama Put Kitchen", "GoodHealth Pharmacy", "Trendy Threads", "CityTech Electronics",
        "Ikeja Grill House", "Divine Touch Salon", "QuickBite Restaurant", "MedPlus Pharmacy",
        "Urban Fashion Hub", "GadgetWorld Electronics", "Ola's Barbershop"
    };

    public static void Seed(LgrrsDbContext db, IDataProtectionCodec codec)
    {
        if (db.Merchants.Any())
        {
            return;
        }

        var random = new Random(42);
        var merchants = new List<Merchant>();

        for (var i = 0; i < BusinessNames.Length; i++)
        {
            var phone = $"080{random.Next(10000000, 99999999)}";
            var merchant = new Merchant
            {
                BusinessName = BusinessNames[i],
                BusinessType = BusinessTypes[i % BusinessTypes.Length],
                LgaCode = "IKEJA",
                LgrrsSystemId = $"LGRRS-{1000 + i}",
                PhoneEncrypted = codec.Encrypt(phone),
                PhoneHash = codec.Hash(phone),
                Status = i % 7 == 0 ? MerchantStatus.UnderReview : MerchantStatus.Verified,
                VerifiedAt = DateTimeOffset.UtcNow.AddDays(-random.Next(5, 60))
            };
            merchants.Add(merchant);
        }

        db.Merchants.AddRange(merchants);

        var drawPeriod = new DrawPeriod
        {
            Type = DrawPeriodType.Weekly,
            StartDate = DateTimeOffset.UtcNow.AddDays(-7),
            EndDate = DateTimeOffset.UtcNow,
            Status = DrawPeriodStatus.Open,
            PrizeBudget = 500_000m
        };
        db.DrawPeriods.Add(drawPeriod);

        var items = new[] { "Haircut", "Groceries", "Lunch Plate", "Medication", "Shirt", "Phone Case", "Suya Platter" };
        var receipts = new List<Receipt>();

        for (var i = 0; i < 60; i++)
        {
            var merchant = merchants[random.Next(merchants.Count)];
            if (merchant.Status != MerchantStatus.Verified)
            {
                continue;
            }

            var phone = $"080{random.Next(10000000, 99999999)}";
            var amount = random.Next(500, 15000);

            var receipt = new Receipt
            {
                MerchantId = merchant.MerchantId,
                CustomerName = $"Customer {i + 1}",
                CustomerPhoneEncrypted = codec.Encrypt(phone),
                CustomerPhoneHash = codec.Hash(phone),
                CustomerPhoneMasked = PhoneMasker.Mask(phone),
                ItemService = items[random.Next(items.Length)],
                Amount = amount,
                Status = random.Next(100) < 5 ? ReceiptStatus.Voided : ReceiptStatus.Valid,
                DrawPeriodId = drawPeriod.DrawPeriodId,
                TransactionDate = DateTimeOffset.UtcNow.AddDays(-random.Next(0, 6)).AddHours(-random.Next(0, 12))
            };
            receipts.Add(receipt);
        }

        db.Receipts.AddRange(receipts);

        var entries = receipts
            .Where(r => r.Status == ReceiptStatus.Valid)
            .Select(r => new RewardEntry
            {
                ReceiptId = r.ReceiptId,
                DrawPeriodId = drawPeriod.DrawPeriodId,
                EligibilityStatus = EligibilityStatus.Eligible,
                RiskStatus = RiskStatus.Clear
            })
            .ToList();

        db.RewardEntries.AddRange(entries);

        // A couple of demo fraud flags so the LGA dashboard has something to show.
        if (receipts.Count > 3)
        {
            db.FraudFlags.AddRange(
                new FraudFlag
                {
                    EntityType = FraudEntityType.Receipt,
                    EntityId = receipts[0].ReceiptId,
                    RuleCode = FraudRuleCode.OutOfHoursSpike,
                    Severity = FraudSeverity.Low,
                    Status = FraudFlagStatus.UnderReview
                },
                new FraudFlag
                {
                    EntityType = FraudEntityType.Merchant,
                    EntityId = merchants[0].MerchantId,
                    RuleCode = FraudRuleCode.HighVelocity,
                    Severity = FraudSeverity.Medium,
                    Status = FraudFlagStatus.Open
                });
        }

        db.AppUsers.Add(new AppUser
        {
            Email = "admin@lgrrs.demo",
            DisplayName = "Ikeja LGA Admin",
            Role = AppRole.LgaAdmin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo#12345")
        });

        db.SaveChanges();
    }
}
