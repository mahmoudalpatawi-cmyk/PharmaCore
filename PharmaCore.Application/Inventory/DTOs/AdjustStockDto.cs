namespace PharmaCore.Application.Inventory.DTOs;

public class AdjustStockDto
{
    public int BatchId { get; set; }
    public int NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}
