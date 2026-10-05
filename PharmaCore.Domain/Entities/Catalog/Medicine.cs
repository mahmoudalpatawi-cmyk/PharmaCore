using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Clinical;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchasing;

namespace PharmaCore.Domain.Entities.Catalog;

/// <summary>
/// Represents the pharmacy's product catalog entry for a medicine.
/// This entity is CATALOG-ONLY: it holds the general product definition.
/// Transactional inventory (stock quantities, batch costs, expiry dates)
/// belongs to the <see cref="Batch"/> entity.
/// 
/// FIXES APPLIED:
/// - Removed MinimumThreshold (duplicate of MinStockLevel).
/// - Removed UnitOfMeasure (redundant third UoM concept alongside PrimaryUnit/SecondaryUnit).
/// - ConversionFactor changed from int to decimal to support fractional conversions
///   (e.g., 1 Box = 2.5 Strips is a real pharmacy scenario).
/// - SellingPrice retained as the default catalog/suggested retail price.
///   The authoritative transactional price is Batch.SellingPrice.
/// </summary>
public class Medicine : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    /// <summary>The outer dispensing unit (e.g., "Box", "Bottle").</summary>
    public string PrimaryUnit { get; set; } = string.Empty;

    /// <summary>The inner dispensing unit (e.g., "Strip", "Tablet"). Null if medicine is sold in primary unit only.</summary>
    public string? SecondaryUnit { get; set; }

    /// <summary>
    /// How many SecondaryUnits are in one PrimaryUnit (e.g., 10 Strips per Box).
    /// Decimal to support fractional conversions (e.g., 2.5).
    /// Only meaningful when SecondaryUnit is set.
    /// </summary>
    public decimal ConversionFactor { get; set; } = 1m;

    /// <summary>Default catalog selling price. Used as a fallback when creating new batches. See Batch.SellingPrice for the transactional price.</summary>
    public decimal SellingPrice { get; set; }

    /// <summary>The stock quantity below which a low-stock alert should be triggered.</summary>
    public int MinStockLevel { get; set; }

    public bool IsScheduleDrug { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<MedicineActiveIngredient> ActiveIngredients { get; set; } = new List<MedicineActiveIngredient>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
