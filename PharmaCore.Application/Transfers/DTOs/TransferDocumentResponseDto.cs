using System;
using System.Collections.Generic;

namespace PharmaCore.Application.Transfers.DTOs;

public class TransferDocumentResponseDto
{
    public int Id { get; set; }
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime TransferDate { get; set; }
    
    public List<TransferDocumentItemResponseDto> Items { get; set; } = new List<TransferDocumentItemResponseDto>();
}

public class TransferDocumentItemResponseDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int Quantity { get; set; }
}
