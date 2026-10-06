using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Transfers.DTOs;

public class UpdateTransferStatusDto
{
    public int TransferDocumentId { get; set; }
    public StockTransferStatus NewStatus { get; set; }
    public string? RejectionReason { get; set; }
}
