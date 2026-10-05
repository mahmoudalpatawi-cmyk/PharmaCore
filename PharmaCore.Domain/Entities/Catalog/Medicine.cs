using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Clinical;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchasing;

namespace PharmaCore.Domain.Entities.Catalog;

public class Medicine : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    public string PrimaryUnit { get; set; } = string.Empty;
    public string? SecondaryUnit { get; set; }
    public string? TertiaryUnit { get; set; }

    public decimal PrimaryToSecondaryConversionFactor { get; set; } = 1m;
    public int SecondaryToTertiaryConversionFactor { get; set; } = 1;

    public decimal SellingPrice { get; set; }

    public int MinStockLevel { get; set; }
    public bool IsScheduleDrug { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<MedicineActiveIngredient> ActiveIngredients { get; set; } = new List<MedicineActiveIngredient>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
