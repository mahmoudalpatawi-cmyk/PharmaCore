using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Purchasing;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Domain.Entities.Inventory;

/// <summary>
/// Represents a specific received shipment (batch/lot) of a medicine at a branch.
/// This is the TRANSACTIONAL INVENTORY entity — it holds the actual stock quantity,
/// cost price, batch-specific selling price, and expiry date.
/// 
/// DESIGN: Quantity has a private setter to enforce domain invariants.
/// All stock mutations MUST go through the domain methods (DeductStock, AddStock).
/// This prevents negative inventory and enforces business rules at the domain layer.
/// A CHECK constraint in the database (Quantity >= 0) provides a second safety net.
/// 
/// EF Core supports private setters natively via reflection during materialization.
/// </summary>
public class Batch : BaseEntity, IMustHaveTenant, ISoftDelete
{
    // EF Core requires a parameterless constructor. Private to enforce use of domain factory methods.
    private Batch() { }

    /// <summary>Use this constructor when creating a new batch from a purchase.</summary>
    public Batch(
        Guid tenantId,
        int branchId,
        int medicineId,
        string batchNumber,
        DateTime expiryDate,
        int initialQuantity,
        decimal costPrice,
        decimal sellingPrice,
        int? supplierId = null)
    {
        if (initialQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialQuantity), "Initial quantity cannot be negative.");

        TenantId = tenantId;
        BranchId = branchId;
        MedicineId = medicineId;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
        Quantity = initialQuantity;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        SupplierId = supplierId;
    }

    public Guid TenantId { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Current stock quantity. Private setter: mutate via DeductStock() or AddStock() only.
    /// A database CHECK constraint (Quantity >= 0) enforces non-negativity at the storage layer.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>The unit cost at which this batch was purchased from the supplier.</summary>
    public decimal CostPrice { get; set; }

    /// <summary>The retail selling price for items from this specific batch. Overrides Medicine.SellingPrice for transactions.</summary>
    public decimal SellingPrice { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Concurrency token (row version) for optimistic concurrency control.
    /// Handled automatically by EF Core / SQL Server rowversion column.
    /// </summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // ─── Domain Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Deducts the specified amount from stock. Throws if stock would go negative.
    /// Called during sale processing, stock adjustments, and transfers.
    /// </summary>
    public void DeductStock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deduction amount must be positive.");
        if (amount > Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock in batch '{BatchNumber}'. Available: {Quantity}, Requested: {amount}.");
        Quantity -= amount;
    }

    /// <summary>
    /// Adds the specified amount to stock.
    /// Called during purchase receiving, sales returns, and inter-branch transfers.
    /// </summary>
    public void AddStock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Addition amount must be positive.");
        Quantity += amount;
    }

    /// <summary>
    /// Adjusts stock to a specific absolute value (e.g., after a physical stock count).
    /// Only use for inventory adjustment operations — not for normal stock mutations.
    /// </summary>
    public void AdjustStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(newQuantity), "Adjusted quantity cannot be negative.");
        Quantity = newQuantity;
    }

    public bool IsExpired => ExpiryDate.Date <= DateTime.UtcNow.Date;
    public bool IsLowStock(int minStockLevel) => Quantity <= minStockLevel;

    // ─── Navigation Collections ───────────────────────────────────────────────

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<SalesReturnItem> SalesReturnItems { get; set; } = new List<SalesReturnItem>();
    public ICollection<PurchaseReturnItem> PurchaseReturnItems { get; set; } = new List<PurchaseReturnItem>();
    public ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();
}
