using PharmaCore.Domain.Common;

namespace PharmaCore.Domain.Entities.Inventory;

public class StockTransferItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int TransferId { get; set; }
    public StockTransfer? Transfer { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int Quantity { get; set; }
}
