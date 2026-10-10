namespace PharmaCore.Application.Catalog.DTOs;

public class MedicineDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;

    public string DosageForm { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string PrimaryUnit { get; set; } = string.Empty;
    public string? SecondaryUnit { get; set; }
    public string? TertiaryUnit { get; set; }

    public decimal PrimaryToSecondaryConversionFactor { get; set; } = 1m;
    public int SecondaryToTertiaryConversionFactor { get; set; } = 1;

    public decimal SellingPrice { get; set; }
    public int MinStockLevel { get; set; }
    public bool IsScheduleDrug { get; set; }
    public bool IsActive { get; set; }
}
