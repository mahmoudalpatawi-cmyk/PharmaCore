using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Finance;

namespace PharmaCore.Domain.Entities.Purchasing;

public class Supplier : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
    public string? TaxNumber { get; set; }
    public decimal Balance { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<PurchaseInvoice> Invoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
    public ICollection<SupplierAccountTransaction> AccountTransactions { get; set; } = new List<SupplierAccountTransaction>();
}
