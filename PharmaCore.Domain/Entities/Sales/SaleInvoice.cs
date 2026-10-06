using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Shifts;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Sales;

public class SaleInvoice : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; private set; } = string.Empty;

    public int? SaleId { get; private set; }
    public Sale? Sale { get; private set; }

    public int? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }

    public int BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    public int ShiftId { get; private set; }
    public Shift? Shift { get; private set; }

    public DateTime InvoiceDate { get; private set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }
    public InvoiceType InvoiceType { get; private set; } = InvoiceType.Sale;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Completed;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<SaleInvoiceItem> Items { get; private set; } = new List<SaleInvoiceItem>();

    private SaleInvoice() { }

    public SaleInvoice(
        Guid tenantId,
        string invoiceNumber,
        int branchId,
        int shiftId,
        PaymentMethod paymentMethod,
        decimal totalAmount,
        decimal taxAmount,
        decimal discountAmount,
        decimal paidAmount,
        int? saleId = null,
        int? customerId = null,
        InvoiceType invoiceType = InvoiceType.Sale)
    {
        TenantId = tenantId;
        InvoiceNumber = invoiceNumber;
        BranchId = branchId;
        ShiftId = shiftId;
        PaymentMethod = paymentMethod;
        TotalAmount = totalAmount;
        TaxAmount = taxAmount;
        DiscountAmount = discountAmount;
        PaidAmount = paidAmount;
        RemainingAmount = totalAmount + taxAmount - discountAmount - paidAmount;
        SaleId = saleId;
        CustomerId = customerId;
        InvoiceType = invoiceType;
        InvoiceDate = DateTime.UtcNow;
        Status = InvoiceStatus.Completed;
    }
}
