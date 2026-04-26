using Doario.Data.Models.Lookups;
using Doario.Data.Models.Mail;
using Doario.Data.Models.SaaS;
using Doario.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data;

public class DoarioDataContext : DbContext
{
    private readonly string _connectionString;

    // Used at runtime by ASP.NET Core dependency injection
    public DoarioDataContext(DbContextOptions<DoarioDataContext> options)
        : base(options)
    {
    }

    // Used by EF Core tools (migrations) via DoarioDataContextFactory
    public DoarioDataContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure manually if not already configured
        // i.e. when called from the factory with a connection string
        // When called by ASP.NET Core it is already configured — skip this
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Tenant
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Domain)
            .IsUnique();

        // TenantSubscription
        modelBuilder.Entity<TenantSubscription>()
            .Property(t => t.MonthlyPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TenantSubscription>()
            .Property(t => t.ExtraDocumentPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TenantSubscription>()
            .Property(t => t.DiscountPercent)
            .HasPrecision(5, 2);

        modelBuilder.Entity<ImportedStaff>().ToTable("ImportedStaff");

        modelBuilder.Entity<Document>()
    .Property(d => d.SenderMatchConfidence)
    .HasPrecision(5, 2);

        modelBuilder.Entity<DocumentAssignment>()
            .Property(d => d.AIConfidence)
            .HasPrecision(5, 2);

        modelBuilder.Entity<DocumentUsage>()
            .Property(d => d.ExtraCharges).HasPrecision(18, 2);
        modelBuilder.Entity<DocumentUsage>()
            .Property(d => d.MonthlyPrice).HasPrecision(18, 2);
        modelBuilder.Entity<DocumentUsage>()
            .Property(d => d.TotalCharged).HasPrecision(18, 2);

        modelBuilder.Entity<TenantSubscription>()
            .Property(d => d.PricePerStaff).HasPrecision(18, 2);

        DoarioDataSeeder.Seed(modelBuilder);
    }
}