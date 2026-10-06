using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Transfers.DTOs;

namespace PharmaCore.Application.Transfers.Interfaces;

public interface ITransferDocumentService
{
    Task<TransferDocumentResponseDto> CreateTransferDocumentAsync(CreateTransferDocumentDto dto, CancellationToken cancellationToken = default);
    Task<TransferDocumentResponseDto> UpdateTransferStatusAsync(UpdateTransferStatusDto dto, CancellationToken cancellationToken = default);
    Task<TransferDocumentResponseDto> GetTransferDocumentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransferDocumentResponseDto>> GetTransferDocumentsByBranchAsync(int branchId, CancellationToken cancellationToken = default);
}
