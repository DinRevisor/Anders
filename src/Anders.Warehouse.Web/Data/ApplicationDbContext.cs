using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Anders.Warehouse.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    private readonly ITenantProvider _tenantProvider;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();
    public DbSet<Agreement> Agreements => Set<Agreement>();
    public DbSet<AgreementRule> AgreementRules => Set<AgreementRule>();
    public DbSet<DeviationReport> DeviationReports => Set<DeviationReport>();
    public DbSet<PriceMonitor> PriceMonitors => Set<PriceMonitor>();
    public DbSet<PriceSourceConfig> PriceSourceConfigs => Set<PriceSourceConfig>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<InAppNotification> Notifications => Set<InAppNotification>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<PurchaseInvoice>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InvoiceId);
        builder.Entity<Agreement>().HasMany(x => x.Rules).WithOne().HasForeignKey(x => x.AgreementId);
        builder.Entity<PriceMonitor>().HasMany(x => x.Sources).WithOne().HasForeignKey(x => x.MonitorId);
    }
}
