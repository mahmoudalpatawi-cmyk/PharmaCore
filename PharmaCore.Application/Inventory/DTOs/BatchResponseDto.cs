using System;

namespace PharmaCore.Application.Inventory.DTOs;

public class BatchResponseDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int MedicineId { get; set; }
    public int? SupplierId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsExpired { get; set; }
}
