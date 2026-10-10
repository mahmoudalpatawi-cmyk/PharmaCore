using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Common.DTOs;
using PharmaCore.Application.Inventory.DTOs;

namespace PharmaCore.Application.Inventory.Interfaces;

public interface IInventoryService
{
    Task<BatchResponseDto> AddBatchAsync(AddBatchDto dto, CancellationToken cancellationToken = default);
    Task TransferStockAsync(StockTransferDto dto, CancellationToken cancellationToken = default);
    Task AdjustStockAsync(AdjustStockDto dto, CancellationToken cancellationToken = default);
    Task<BatchResponseDto> GetBatchAsync(int batchId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<StockMovementResponseDto>> GetStockMovementsAsync(
        int batchId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
