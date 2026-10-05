using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchasing;
using PharmaCore.Domain.Entities.Sales;
using PharmaCore.Domain.Entities.Shifts;

namespace PharmaCore.Domain.Entities.MultiTenancy;

public class Branch : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? ManagerName { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Tenant? Tenant { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
    public ICollection<StockTransfer> OutgoingTransfers { get; set; } = new List<StockTransfer>();
    public ICollection<StockTransfer> IncomingTransfers { get; set; } = new List<StockTransfer>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
    public ICollection<SaleInvoice> SaleInvoices { get; set; } = new List<SaleInvoice>();
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
