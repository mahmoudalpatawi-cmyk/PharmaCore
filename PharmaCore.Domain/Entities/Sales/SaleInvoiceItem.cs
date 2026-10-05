using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Domain.Entities.Sales;

/// <summary>
/// A line item on a SaleInvoice, referencing a specific Batch for pricing and stock source.
/// 
/// FIXES APPLIED:
/// - Removed the MedicineBatchId and MedicineBatch alias properties. These property
///   wrappers (using `Batch as MedicineBatch`) always returned null at runtime because
///   EF Core materializes Batch objects, never MedicineBatch objects (now that the TPH
///   ghost entity is removed). Use BatchId and Batch directly.
/// </summary>
public class SaleInvoiceItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int SaleInvoiceId { get; set; }
    public SaleInvoice? SaleInvoice { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
}
