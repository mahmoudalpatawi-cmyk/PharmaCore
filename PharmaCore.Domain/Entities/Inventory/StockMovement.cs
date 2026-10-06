using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Inventory;

/// <summary>
/// Immutable ledger record of every stock quantity change for a batch at a branch.
/// Provides a full audit trail of how Batch.Quantity arrived at its current value.
/// 
/// FIXES APPLIED:
/// - Removed the MedicineBatchId and MedicineBatch alias properties. These were
///   wrappers around BatchId/Batch that always returned null at runtime because
///   EF Core materialized a Batch object (never a MedicineBatch after TPH was fixed).
///   Directly use BatchId and Batch navigation property.
/// </summary>
public class StockMovement : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public StockMovementType MovementType { get; set; }

    /// <summary>Negative for outflows (sales, transfers out), positive for inflows (purchases, returns in).</summary>
    public int QuantityChanged { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }

    public string? Reason { get; set; }

    /// <summary>Reference to the source document (Invoice ID, Transfer ID, etc.) stored as string to support multiple ID types.</summary>
    public string? ReferenceDocumentId { get; set; }
}
