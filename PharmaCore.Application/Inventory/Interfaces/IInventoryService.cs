using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Inventory.DTOs;

namespace PharmaCore.Application.Inventory.Interfaces;

public interface IInventoryService
{
    Task<BatchResponseDto> AddBatchAsync(AddBatchDto dto, CancellationToken cancellationToken = default);
    Task TransferStockAsync(StockTransferDto dto, CancellationToken cancellationToken = default);
    Task AdjustStockAsync(AdjustStockDto dto, CancellationToken cancellationToken = default);
    Task<BatchResponseDto> GetBatchAsync(int batchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockMovementResponseDto>> GetStockMovementsAsync(int batchId, CancellationToken cancellationToken = default);
}
