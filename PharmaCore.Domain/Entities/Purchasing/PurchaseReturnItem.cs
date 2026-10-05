using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Domain.Entities.Purchasing;

public class PurchaseReturnItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int Quantity { get; set; }
}
