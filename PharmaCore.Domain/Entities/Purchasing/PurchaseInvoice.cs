using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Purchasing;

public class PurchaseInvoice : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;

    public int SupplierId { get; private set; }
    public Supplier? Supplier { get; private set; }

    public int BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    public DateTime InvoiceDate { get; private set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Completed;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PurchaseInvoiceItem> Items { get; private set; } = new List<PurchaseInvoiceItem>();

    private PurchaseInvoice() { }

    public PurchaseInvoice(
        Guid tenantId,
        string invoiceNumber,
        int supplierId,
        int branchId,
        PaymentMethod paymentMethod,
        decimal totalAmount,
        decimal taxAmount,
        decimal discountAmount,
        decimal paidAmount)
    {
        TenantId = tenantId;
        InvoiceNumber = invoiceNumber;
        SupplierId = supplierId;
        BranchId = branchId;
        PaymentMethod = paymentMethod;
        TotalAmount = totalAmount;
        TaxAmount = taxAmount;
        DiscountAmount = discountAmount;
        PaidAmount = paidAmount;
        RemainingAmount = totalAmount + taxAmount - discountAmount - paidAmount;
        InvoiceDate = DateTime.UtcNow;
        Status = InvoiceStatus.Completed;
    }
}
