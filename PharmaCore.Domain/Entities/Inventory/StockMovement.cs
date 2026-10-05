using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Inventory;

public class StockMovement : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

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

    public StockMovementType MovementType { get; set; }
    public int QuantityChanged { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceInvoiceId { get; set; }
}
