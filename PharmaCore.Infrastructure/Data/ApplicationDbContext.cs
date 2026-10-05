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
using System.Reflection;

namespace PharmaCore.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

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
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

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

    private void ApplyAuditInformation()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
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
        
        var guidEntries = ChangeTracker.Entries<BaseEntity<Guid>>();

        foreach (var entry in guidEntries)
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
