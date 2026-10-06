namespace PharmaCore.Application.Catalog.DTOs;

public class CreateMedicineDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string? Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
}
