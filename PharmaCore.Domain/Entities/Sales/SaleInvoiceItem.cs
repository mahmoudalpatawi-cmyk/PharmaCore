using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Domain.Entities.Sales;

public class SaleInvoiceItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int SaleInvoiceId { get; set; }
    public SaleInvoice? SaleInvoice { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int MedicineBatchId
    {
        get => BatchId;
        set => BatchId = value;
    }
    public MedicineBatch? MedicineBatch
    {
        get => Batch as MedicineBatch;
        set => Batch = value;
    }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
}
