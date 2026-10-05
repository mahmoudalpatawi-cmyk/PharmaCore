using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Domain.Entities.Purchasing;

/// <summary>
/// A line item on a PurchaseInvoice representing one medicine received in a specific batch.
/// 
/// FIX APPLIED: Added BatchId FK linking this invoice line to the Batch entity created
/// when goods were received. Previously these were unlinked — the BatchNumber was a free
/// string with no referential integrity to the Batch table, making the question
/// "which invoice produced which batch?" impossible to answer programmatically.
/// 
/// BatchId is nullable because: at the time of invoice entry the batch may not yet exist
/// (goods not received). On goods-receiving, BatchId is populated.
/// </summary>
public class PurchaseInvoiceItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    /// <summary>
    /// Link to the Batch created when this invoice line's goods were received into inventory.
    /// Null until goods-receiving is confirmed.
    /// </summary>
    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }

    /// <summary>The supplier's batch/lot number printed on the packaging.</summary>
    public string BatchNumber { get; set; } = string.Empty;

    public DateTime ExpirationDate { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal SubTotal { get; set; }
}
