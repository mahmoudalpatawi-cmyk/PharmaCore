using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;

namespace PharmaCore.Domain.Entities.Purchasing;

public class PurchaseOrderItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
