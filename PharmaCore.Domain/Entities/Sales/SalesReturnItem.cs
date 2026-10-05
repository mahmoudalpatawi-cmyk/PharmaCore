using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Domain.Entities.Sales;

public class SalesReturnItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int SalesReturnId { get; set; }
    public SalesReturn? SalesReturn { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int Quantity { get; set; }
    public string UnitReturned { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}
