using System;

namespace PharmaCore.Application.Inventory.DTOs;

public class AddBatchDto
{
    public int BranchId { get; set; }
    public int MedicineId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int InitialQuantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int? SupplierId { get; set; }
}
