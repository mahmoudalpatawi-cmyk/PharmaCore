using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Analytics;
using PharmaCore.Domain.Entities.Auditing;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Clinical;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Insurance;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Purchasing;
using PharmaCore.Domain.Entities.Sales;
using PharmaCore.Domain.Entities.Shifts;

namespace PharmaCore.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// The tenant ID resolved from the current HTTP request (via ITenantProvider).
    /// Captured once per DbContext lifetime (= once per DI scope = once per request).
    /// Referenced as a closure in HasQueryFilter expressions, which EF Core evaluates
    /// at query time from the current DbContext instance — providing correct per-request
    /// tenant isolation without the performance cost of resolving the tenant on every query.
    /// </summary>
    private readonly Guid _currentTenantId;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider) : base(options)
    {
        _currentTenantId = tenantProvider.GetTenantId();
    }

    // ─── DbSets ───────────────────────────────────────────────────────────────

    // Analytics
    public DbSet<AiForecastLog> AiForecastLogs => Set<AiForecastLog>();

    // Auditing
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Catalog
    public DbSet<ActiveIngredient> ActiveIngredients => Set<ActiveIngredient>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineActiveIngredient> MedicineActiveIngredients => Set<MedicineActiveIngredient>();

    // Clinical
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();

    // Finance
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<CustomerAccountTransaction> CustomerAccountTransactions => Set<CustomerAccountTransaction>();
    public DbSet<SupplierAccountTransaction> SupplierAccountTransactions => Set<SupplierAccountTransaction>();

    // Identity
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();

    // Insurance
    public DbSet<CustomerInsurance> CustomerInsurances => Set<CustomerInsurance>();
    public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();

    // Inventory
    public DbSet<Batch> Batches => Set<Batch>();
    //public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    // NOTE: MedicineBatches DbSet intentionally removed. MedicineBatch is a deprecated
    // alias class that caused EF Core TPH Discriminator issues. Use Batches instead.

    // MultiTenancy
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    // Purchasing
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // Sales
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleInvoice> SaleInvoices => Set<SaleInvoice>();
    public DbSet<SaleInvoiceItem> SaleInvoiceItems => Set<SaleInvoiceItem>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();

    // Shifts
    public DbSet<Shift> Shifts => Set<Shift>();

    // ─── Model Configuration ──────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Exclude the deprecated MedicineBatch alias class from the EF Core model entirely.
        // Without this, EF Core would add a Discriminator column to the Batches table (TPH).
        modelBuilder.Ignore<MedicineBatch>();

        // Apply all IEntityTypeConfiguration<T> classes discovered in this assembly.
        // These handle: property MaxLength, decimal precision, indexes, and FK relationships.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply global query filters AFTER configurations, so all entity types are registered.
        // Filters are applied per-interface: IMustHaveTenant, ISoftDelete, or both.
        ApplyGlobalQueryFilters(modelBuilder);

        // Safety net: override any Cascade delete behaviors that were not explicitly
        // set to Restrict in individual configurations. This prevents accidental
        // historical data destruction (e.g., deleting a Supplier cascading to Batches).
        foreach (var fk in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    // ─── Global Query Filter Infrastructure ──────────────────────────────────

    /// <summary>
    /// Iterates every entity type in the model and applies the appropriate query filter
    /// based on which multi-tenancy/soft-delete interfaces the entity implements.
    /// 
    /// Three cases:
    ///   IMustHaveTenant + ISoftDelete → filter on TenantId AND !IsDeleted
    ///   IMustHaveTenant only          → filter on TenantId only
    ///   ISoftDelete only              → filter on !IsDeleted only (e.g., Tenant itself)
    /// 
    /// The generic private methods are invoked via reflection to satisfy EF Core's
    /// requirement for strongly-typed HasQueryFilter<T> expressions.
    /// The closure captures `_currentTenantId` from the current DbContext instance,
    /// which EF Core re-evaluates per query (not once at model build time).
    /// </summary>
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenant = typeof(IMustHaveTenant).IsAssignableFrom(clrType);
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);

            if (isTenant && isSoftDelete)
            {
                GetType()
                    .GetMethod(nameof(ApplyTenantAndSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType)
                    .Invoke(this, new object[] { modelBuilder });
            }
            else if (isTenant)
            {
                GetType()
                    .GetMethod(nameof(ApplyTenantOnlyFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType)
                    .Invoke(this, new object[] { modelBuilder });
            }
            else if (isSoftDelete)
            {
                GetType()
                    .GetMethod(nameof(ApplySoftDeleteOnlyFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType)
                    .Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void ApplyTenantAndSoftDeleteFilter<T>(ModelBuilder modelBuilder)
        where T : class, IMustHaveTenant, ISoftDelete
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _currentTenantId && !e.IsDeleted);
    }

    private void ApplyTenantOnlyFilter<T>(ModelBuilder modelBuilder)
        where T : class, IMustHaveTenant
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _currentTenantId);
    }

    private void ApplySoftDeleteOnlyFilter<T>(ModelBuilder modelBuilder)
        where T : class, ISoftDelete
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    // ─── Save Interceptors ────────────────────────────────────────────────────

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stamps CreatedAt/UpdatedAt on all auditable entities before saving.
    /// 
    /// FIX: Uses a single loop over IAuditableEntity (implemented by BaseEntity&lt;TId&gt;).
    /// The previous implementation used two separate loops — one for BaseEntity&lt;int&gt;
    /// and one for BaseEntity&lt;Guid&gt; — causing all int-keyed entities to be stamped twice.
    /// This unified loop eliminates the double-processing bug.
    /// </summary>
    private void ApplyAuditInformation()
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
