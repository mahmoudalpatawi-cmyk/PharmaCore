using System;

namespace PharmaCore.Application.Inventory.DTOs;

public class StockMovementResponseDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int BatchId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int QuantityChanged { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceDocumentId { get; set; }
    public DateTime CreatedAt { get; set; }
}
