namespace PharmaCore.Application.Inventory.DTOs;

public class StockTransferDto
{
    public int SourceBatchId { get; set; }
    public int SourceBranchId { get; set; }
    public int DestinationBatchId { get; set; }
    public int DestinationBranchId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}
