using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Purchasing;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Domain.Entities.Inventory;

public class Batch : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<SalesReturnItem> SalesReturnItems { get; set; } = new List<SalesReturnItem>();
    public ICollection<PurchaseReturnItem> PurchaseReturnItems { get; set; } = new List<PurchaseReturnItem>();
}
