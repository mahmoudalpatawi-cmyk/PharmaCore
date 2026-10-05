using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Purchasing;

public class PurchaseInvoice : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Completed;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
}
