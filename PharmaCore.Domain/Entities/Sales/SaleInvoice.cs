using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Shifts;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Sales;

public class SaleInvoice : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public InvoiceType InvoiceType { get; set; } = InvoiceType.Sale;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Completed;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<SaleInvoiceItem> Items { get; set; } = new List<SaleInvoiceItem>();
}
