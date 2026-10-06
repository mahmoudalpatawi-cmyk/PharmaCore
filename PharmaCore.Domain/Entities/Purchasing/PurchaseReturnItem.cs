using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Domain.Entities.Purchasing;

/// <summary>
/// A line item on a PurchaseReturn representing goods being returned to a supplier.
/// 
/// FIX APPLIED: Added UnitPrice, TotalAmount, and Reason fields.
/// Previously this entity only stored Quantity, making supplier credit memos
/// financially meaningless — there was no way to verify PurchaseReturn.TotalAmount
/// from its children, nor produce an itemized credit note.
/// </summary>
public class PurchaseReturnItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int Quantity { get; set; }

    /// <summary>The unit price at which this item is being credited back (typically matches the original purchase price).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity × UnitPrice. Must match sum of parent PurchaseReturn.TotalAmount.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Reason for returning this specific item (e.g., "Damaged", "Expired", "Wrong Item Supplied").</summary>
    public string? Reason { get; set; }
}
