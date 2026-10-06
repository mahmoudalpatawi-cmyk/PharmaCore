using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Inventory.DTOs;
using PharmaCore.Application.Inventory.Interfaces;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Enums;

// Note: Assuming standard repository/UOW namespace mapping
// using PharmaCore.Application.Common.Interfaces;

namespace PharmaCore.Application.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly IRepository<Batch> _batchRepository;
    private readonly IRepository<StockMovement> _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public InventoryService(
        IRepository<Batch> batchRepository,
        IRepository<StockMovement> stockMovementRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _stockMovementRepository = stockMovementRepository ?? throw new ArgumentNullException(nameof(stockMovementRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<BatchResponseDto> AddBatchAsync(AddBatchDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var batch = new Batch(
            tenantId,
            dto.BranchId,
            dto.MedicineId,
            dto.BatchNumber,
            dto.ExpiryDate,
            dto.InitialQuantity,
            dto.CostPrice,
            dto.SellingPrice,
            dto.SupplierId
        );

        await _batchRepository.AddAsync(batch, cancellationToken);

        if (dto.InitialQuantity > 0)
        {
            var movement = new StockMovement
            {
                TenantId = tenantId,
                BranchId = dto.BranchId,
                MovementType = StockMovementType.Purchase, 
                QuantityBefore = 0,
                QuantityChanged = dto.InitialQuantity,
                QuantityAfter = dto.InitialQuantity,
                Reason = "Opening Balance / Direct Batch Addition"
            };
            batch.StockMovements.Add(movement);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BatchResponseDto>(batch);
    }

    public async Task TransferStockAsync(StockTransferDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var sourceBatch = await _batchRepository.GetByIdAsync(dto.SourceBatchId, cancellationToken);
        var destBatch = await _batchRepository.GetByIdAsync(dto.DestinationBatchId, cancellationToken);

        if (sourceBatch == null || sourceBatch.TenantId != tenantId)
            throw new UnauthorizedAccessException("Source batch not found or unauthorized.");

        if (destBatch == null || destBatch.TenantId != tenantId)
            throw new UnauthorizedAccessException("Destination batch not found or unauthorized.");

        if (sourceBatch.BranchId != dto.SourceBranchId)
            throw new InvalidOperationException("Source batch does not match the expected source branch.");

        if (destBatch.BranchId != dto.DestinationBranchId)
            throw new InvalidOperationException("Destination batch does not match the expected destination branch.");

        if (sourceBatch.Id == destBatch.Id)
            throw new InvalidOperationException("Cannot transfer stock into the same batch.");

        int sourceBefore = sourceBatch.Quantity;
        int destBefore = destBatch.Quantity;

        sourceBatch.DeductStock(dto.Quantity);
        destBatch.AddStock(dto.Quantity);

        var sourceMovement = new StockMovement
        {
            TenantId = tenantId,
            BranchId = sourceBatch.BranchId,
            BatchId = sourceBatch.Id,
            MovementType = StockMovementType.TransferOut, 
            QuantityBefore = sourceBefore,
            QuantityChanged = -dto.Quantity,
            QuantityAfter = sourceBatch.Quantity,
            Reason = dto.Reason ?? $"Transferred out to Batch {destBatch.BatchNumber} at Branch {destBatch.BranchId}"
        };

        var destMovement = new StockMovement
        {
            TenantId = tenantId,
            BranchId = destBatch.BranchId,
            BatchId = destBatch.Id,
            MovementType = StockMovementType.TransferIn,
            QuantityBefore = destBefore,
            QuantityChanged = dto.Quantity,
            QuantityAfter = destBatch.Quantity,
            Reason = dto.Reason ?? $"Transferred in from Batch {sourceBatch.BatchNumber} at Branch {sourceBatch.BranchId}"
        };

        await _stockMovementRepository.AddAsync(sourceMovement, cancellationToken);
        await _stockMovementRepository.AddAsync(destMovement, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AdjustStockAsync(AdjustStockDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var batch = await _batchRepository.GetByIdAsync(dto.BatchId, cancellationToken);

        if (batch == null || batch.TenantId != tenantId)
            throw new UnauthorizedAccessException("Batch not found or unauthorized.");

        int quantityBefore = batch.Quantity;

        batch.AdjustStock(dto.NewQuantity);

        int quantityChanged = batch.Quantity - quantityBefore;

        var movement = new StockMovement
        {
            TenantId = tenantId,
            BranchId = batch.BranchId,
            BatchId = batch.Id,
            MovementType = StockMovementType.Adjustment,
            QuantityBefore = quantityBefore,
            QuantityChanged = quantityChanged,
            QuantityAfter = batch.Quantity,
            Reason = dto.Reason
        };

        await _stockMovementRepository.AddAsync(movement, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<BatchResponseDto> GetBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var batch = await _batchRepository.GetByIdAsync(batchId, cancellationToken);

        if (batch == null || batch.TenantId != tenantId)
            throw new UnauthorizedAccessException("Batch not found or unauthorized.");

        return _mapper.Map<BatchResponseDto>(batch);
    }

    public async Task<IEnumerable<StockMovementResponseDto>> GetStockMovementsAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var batch = await _batchRepository.GetByIdAsync(batchId, cancellationToken);

        if (batch == null || batch.TenantId != tenantId)
            throw new UnauthorizedAccessException("Batch not found or unauthorized.");

        var movements = await _stockMovementRepository.ListAsync(
            m => m.BatchId == batchId && m.TenantId == tenantId,
            cancellationToken);

        return _mapper.Map<IEnumerable<StockMovementResponseDto>>(movements);
    }
}
