namespace PharmaCore.Application.Catalog.DTOs;

public class UpdateMedicineDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int ManufacturerId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    
    public string DosageForm { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    
    public string? Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsActive { get; set; }
}
