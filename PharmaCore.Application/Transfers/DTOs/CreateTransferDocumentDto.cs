using System.Collections.Generic;

namespace PharmaCore.Application.Transfers.DTOs;

public class CreateTransferDocumentDto
{
    public int SourceBranchId { get; set; }
    public int DestinationBranchId { get; set; }
    public string? Notes { get; set; }
    public List<TransferDocumentItemDto> Items { get; set; } = new List<TransferDocumentItemDto>();
}
