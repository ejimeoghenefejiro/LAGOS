using LGRRS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Infrastructure.Persistence;

public class LgrrsDbContext : DbContext
{
    public LgrrsDbContext(DbContextOptions<LgrrsDbContext> options) : base(options)
    {
    }

    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<PosSubmission> PosSubmissions => Set<PosSubmission>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptDelivery> ReceiptDeliveries => Set<ReceiptDelivery>();
    public DbSet<DrawPeriod> DrawPeriods => Set<DrawPeriod>();
    public DbSet<RewardEntry> RewardEntries => Set<RewardEntry>();
    public DbSet<DrawResult> DrawResults => Set<DrawResult>();
    public DbSet<PrizeClaim> PrizeClaims => Set<PrizeClaim>();
    public DbSet<FraudFlag> FraudFlags => Set<FraudFlag>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<SaleReport> SaleReports => Set<SaleReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PosSubmission>(e => {
            e.HasKey(s => s.PosSubmissionId);
            e.HasIndex(s => new { s.MerchantId, s.ExternalSaleId }).IsUnique();
            e.HasOne(s => s.Merchant).WithMany().HasForeignKey(s => s.MerchantId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<CatalogItem>(e => {
            e.HasKey(i => i.CatalogItemId);
            e.Property(i => i.Price).HasColumnType("decimal(18,2)");
            e.HasIndex(i => new { i.MerchantId, i.Name }).IsUnique();
            e.HasOne(i => i.Merchant).WithMany().HasForeignKey(i => i.MerchantId);
        });
        modelBuilder.Entity<OtpChallenge>(e =>
        {
            e.HasKey(o => o.OtpChallengeId);
            e.HasIndex(o => new { o.PhoneHash, o.Purpose }).IsUnique();
            e.HasOne(o => o.Merchant).WithMany().HasForeignKey(o => o.MerchantId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<SaleReport>(e =>
        {
            e.HasKey(r => r.SaleReportId);
            e.Property(r => r.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(r => r.Fingerprint).IsUnique();
            e.HasIndex(r => new { r.ReporterPhoneHash, r.CreatedAt });
        });
        modelBuilder.Entity<Merchant>(e =>
        {
            e.HasKey(m => m.MerchantId);
            e.Property(m => m.PosApiKeyHash).HasMaxLength(64);
            e.HasIndex(m => m.PosApiKeyHash).IsUnique().HasFilter("[PosApiKeyHash] IS NOT NULL");
            e.Property(m => m.PosApiKeyPrefix).HasMaxLength(20);
            e.Property(m => m.BusinessName).HasMaxLength(200).IsRequired();
            e.Property(m => m.BusinessType).HasMaxLength(100).IsRequired();
            e.Property(m => m.LgaCode).HasMaxLength(50).IsRequired();
            e.Property(m => m.LgrrsSystemId).HasMaxLength(50);
            e.Property(m => m.LagosTaxIdEncrypted).HasMaxLength(500);
            e.Property(m => m.PhoneEncrypted).HasMaxLength(500).IsRequired();
            e.Property(m => m.PhoneHash).HasMaxLength(64).IsRequired();
            e.HasIndex(m => m.PhoneHash);
            e.HasIndex(m => m.LgrrsSystemId).IsUnique().HasFilter("[LgrrsSystemId] IS NOT NULL");
        });

        modelBuilder.Entity<Receipt>(e =>
        {
            e.HasKey(r => r.ReceiptId);
            e.Property(r => r.PaperClaimHash).HasMaxLength(64);
            e.HasIndex(r => r.PaperClaimHash).IsUnique().HasFilter("[PaperClaimHash] IS NOT NULL");
            e.Property(r => r.ItemService).HasMaxLength(200).IsRequired();
            e.Property(r => r.Amount).HasColumnType("decimal(18,2)");
            e.Property(r => r.CustomerPhoneHash).HasMaxLength(64);
            e.HasIndex(r => r.CustomerPhoneHash);
            e.Ignore(r => r.ReceiptRefMasked);
            e.HasOne(r => r.Merchant).WithMany(m => m.Receipts).HasForeignKey(r => r.MerchantId);
            e.HasOne(r => r.DrawPeriod).WithMany().HasForeignKey(r => r.DrawPeriodId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ReceiptDelivery>(e =>
        {
            e.HasKey(d => d.DeliveryId);
            e.Property(d => d.DestinationMasked).HasMaxLength(50).IsRequired();
            e.HasOne(d => d.Receipt).WithMany(r => r.Deliveries).HasForeignKey(d => d.ReceiptId);
        });

        modelBuilder.Entity<DrawPeriod>(e =>
        {
            e.HasKey(d => d.DrawPeriodId);
            e.Property(d => d.PrizeBudget).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<RewardEntry>(e =>
        {
            e.HasKey(r => r.EntryId);
            e.HasOne(r => r.Receipt).WithOne(rc => rc.RewardEntry).HasForeignKey<RewardEntry>(r => r.ReceiptId);
            e.HasOne(r => r.DrawPeriod).WithMany(d => d.RewardEntries).HasForeignKey(r => r.DrawPeriodId).OnDelete(DeleteBehavior.NoAction);
            // Spec 13.2: one receipt may create only one active reward entry per applicable draw.
            e.HasIndex(r => new { r.ReceiptId, r.DrawPeriodId }).IsUnique();
        });

        modelBuilder.Entity<DrawResult>(e =>
        {
            e.HasKey(r => r.DrawResultId);
            e.Property(r => r.PrizeTier).HasMaxLength(50).IsRequired();
            e.Property(r => r.PrizeAmount).HasColumnType("decimal(18,2)");
            e.HasOne(r => r.DrawPeriod).WithMany(d => d.DrawResults).HasForeignKey(r => r.DrawPeriodId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(r => r.Entry).WithOne(en => en.DrawResult).HasForeignKey<DrawResult>(r => r.EntryId);
        });

        modelBuilder.Entity<PrizeClaim>(e =>
        {
            e.HasKey(c => c.ClaimId);
            e.Property(c => c.ClaimRef).HasMaxLength(30).IsRequired();
            e.HasIndex(c => c.ClaimRef).IsUnique();
            e.Property(c => c.PhoneHash).HasMaxLength(64).IsRequired();
            e.HasOne(c => c.DrawResult).WithOne(r => r.PrizeClaim).HasForeignKey<PrizeClaim>(c => c.DrawResultId);
        });

        modelBuilder.Entity<FraudFlag>(e =>
        {
            e.HasKey(f => f.FlagId);
            e.HasIndex(f => new { f.EntityType, f.EntityId });
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.HasKey(a => a.EventId);
            e.Property(a => a.EventType).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            e.HasIndex(a => new { a.EntityType, a.EntityId });
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
        });
    }
}
