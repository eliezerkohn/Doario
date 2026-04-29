using Doario.Data.Models.Lookups;
using Doario.Data.Models.Mail;
using Doario.Data.Models.SaaS;
using Doario.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data;

public class DoarioDataContext : DbContext
{
    private readonly string _connectionString;

    public DoarioDataContext(DbContextOptions<DoarioDataContext> options)
        : base(options) { }

    public DoarioDataContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_connectionString);
    }

    // SaaS
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantSubscription> TenantSubscriptions { get; set; }
    public DbSet<TenantConnection> TenantConnections { get; set; }
    public DbSet<DocumentUsage> DocumentUsages { get; set; }
    public DbSet<StaffSyncLog> StaffSyncLogs { get; set; }
    public DbSet<TenantConnectorConfig> TenantConnectorConfigs { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }  // ← NEW

    // Lookups
    public DbSet<DocumentStatus> DocumentStatuses { get; set; }
    public DbSet<SenderType> SenderTypes { get; set; }
    public DbSet<AssignmentType> AssignmentTypes { get; set; }
    public DbSet<NotificationType> NotificationTypes { get; set; }
    public DbSet<Sender> Senders { get; set; }
    public DbSet<SourceType> SourceTypes { get; set; }
    public DbSet<SystemStatus> SystemStatuses { get; set; }
    public DbSet<MessageType> MessageTypes { get; set; }

    // Mail
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentAssignment> DocumentAssignments { get; set; }
    public DbSet<DocumentDelivery> DocumentDeliveries { get; set; }
    public DbSet<ImportedStaff> ImportedStaff { get; set; }
    public DbSet<ImportedSender> ImportedSenders { get; set; }
    public DbSet<DocumentMessage> DocumentMessages { get; set; }
    public DbSet<DocumentFeedback> DocumentFeedbacks { get; set; }
    public DbSet<TenantWhitelistedSender> TenantWhitelistedSenders { get; set; }
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<DocumentViewed> DocumentVieweds { get; set; }
    public DbSet<TenantExtractionField> TenantExtractionFields { get; set; }
    public DbSet<DocumentCheck> DocumentChecks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Domain)
            .IsUnique();

        // ── TenantSubscription ───────────────────────────────────────
        modelBuilder.Entity<TenantSubscription>()
            .Property(t => t.MonthlyPrice).HasPrecision(18, 2);
        modelBuilder.Entity<TenantSubscription>()
            .Property(t => t.ExtraDocumentPrice).HasPrecision(18, 4);  // 4dp for extra doc price
        modelBuilder.Entity<TenantSubscription>()
            .Property(t => t.DiscountPercent).HasPrecision(5, 2);
        modelBuilder.Entity<TenantSubscription>()
            .Property(d => d.PricePerStaff).HasPrecision(18, 2);
        modelBuilder.Entity<TenantSubscription>()
            .HasOne(ts => ts.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(ts => ts.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);                         // ← NEW FK

        // ── SubscriptionPlan ─────────────────────────────────────────
        modelBuilder.Entity<SubscriptionPlan>()
            .Property(p => p.MonthlyPrice).HasPrecision(18, 2);
        modelBuilder.Entity<SubscriptionPlan>()
            .Property(p => p.ExtraDocumentPrice).HasPrecision(18, 4);  // ← NEW

        // ── Document ─────────────────────────────────────────────────
        modelBuilder.Entity<ImportedStaff>().ToTable("ImportedStaff");

        modelBuilder.Entity<Document>()
            .Property(d => d.SenderMatchConfidence).HasPrecision(5, 2);

        modelBuilder.Entity<DocumentAssignment>()
            .Property(d => d.AIConfidence).HasPrecision(5, 2);

        // ── DocumentUsage ────────────────────────────────────────────
        modelBuilder.Entity<DocumentUsage>()
            .Property(d => d.ExtraCharges).HasPrecision(18, 2);
        modelBuilder.Entity<DocumentUsage>()
            .Property(d => d.MonthlyPrice).HasPrecision(18, 2);
        modelBuilder.Entity<DocumentUsage>()
            .Property(d => d.TotalCharged).HasPrecision(18, 2);

        // ── TenantExtractionField ─────────────────────────────────────
        modelBuilder.Entity<TenantExtractionField>(e =>
        {
            e.HasKey(x => x.TenantExtractionFieldId);

            e.Property(x => x.FieldName)
                .IsRequired()
                .HasMaxLength(100);

            e.Property(x => x.FieldDescription)
                .IsRequired()
                .HasMaxLength(500)
                .HasDefaultValue("");

            e.Property(x => x.EndDate)
                .HasDefaultValue(DateTime.MaxValue);

            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DocumentCheck ─────────────────────────────────────────────
        modelBuilder.Entity<DocumentCheck>(e =>
        {
            e.HasKey(x => x.DocumentCheckId);

            e.Property(x => x.CheckAmount)
                .HasColumnType("decimal(18,2)");

            e.Property(x => x.CheckPayerName)
                .IsRequired()
                .HasMaxLength(200);

            e.Property(x => x.CheckNumber)
                .IsRequired()
                .HasMaxLength(50);

            e.HasOne(x => x.Document)
                .WithOne()
                .HasForeignKey<DocumentCheck>(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        DoarioDataSeeder.Seed(modelBuilder);
    }
}