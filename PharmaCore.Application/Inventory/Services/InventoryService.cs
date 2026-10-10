using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Common.DTOs;
using PharmaCore.Application.Common.Exceptions;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Application.Inventory.DTOs;
using PharmaCore.Application.Inventory.Interfaces;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly IRepository<Batch> _batchRepository;
    private readonly IRepository<StockMovement> _stockMovementRepository;
    private readonly IRepository<Medicine> _medicineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public InventoryService(
        IRepository<Batch> batchRepository,
        IRepository<StockMovement> stockMovementRepository,
        IRepository<Medicine> medicineRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _stockMovementRepository = stockMovementRepository ?? throw new ArgumentNullException(nameof(stockMovementRepository));
        _medicineRepository = medicineRepository ?? throw new ArgumentNullException(nameof(medicineRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<BatchResponseDto> AddBatchAsync(AddBatchDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.CanAccessBranch(dto.BranchId))
        {
            throw new UnauthorizedAccessException($"User is not authorized to access branch {dto.BranchId}.");
        }

        var tenantId = _tenantProvider.GetTenantId();

        var medicine = await _medicineRepository.GetByIdAsync(dto.MedicineId, cancellationToken);
        if (medicine == null || medicine.TenantId != tenantId || medicine.IsDeleted)
        {
            throw new NotFoundException($"Medicine with ID {dto.MedicineId} was not found.");
        }

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

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new ConflictException("The batch record was modified concurrently by another process. Please reload and retry.");
        }

        return _mapper.Map<BatchResponseDto>(batch);
    }

    public async Task TransferStockAsync(StockTransferDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.CanAccessBranch(dto.SourceBranchId))
        {
            throw new UnauthorizedAccessException($"User is not authorized to access source branch {dto.SourceBranchId}.");
        }

        if (!_currentUserService.CanAccessBranch(dto.DestinationBranchId))
        {
            throw new UnauthorizedAccessException($"User is not authorized to access destination branch {dto.DestinationBranchId}.");
        }

        var tenantId = _tenantProvider.GetTenantId();

        var sourceBatch = await _batchRepository.GetByIdAsync(dto.SourceBatchId, cancellationToken);
        var destBatch = await _batchRepository.GetByIdAsync(dto.DestinationBatchId, cancellationToken);

        if (sourceBatch == null || sourceBatch.TenantId != tenantId)
            throw new NotFoundException($"Source batch with ID {dto.SourceBatchId} was not found.");

        if (destBatch == null || destBatch.TenantId != tenantId)
            throw new NotFoundException($"Destination batch with ID {dto.DestinationBatchId} was not found.");

        if (!_currentUserService.CanAccessBranch(sourceBatch.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to access source branch {sourceBatch.BranchId}.");

        if (!_currentUserService.CanAccessBranch(destBatch.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to access destination branch {destBatch.BranchId}.");

        if (sourceBatch.BranchId != dto.SourceBranchId)
            throw new InvalidOperationException("Source batch does not match the expected source branch.");

        if (destBatch.BranchId != dto.DestinationBranchId)
            throw new InvalidOperationException("Destination batch does not match the expected destination branch.");

        if (sourceBatch.BranchId == destBatch.BranchId)
            throw new InvalidOperationException("Source and destination batches must belong to different branches.");

        if (sourceBatch.Id == destBatch.Id)
            throw new InvalidOperationException("Cannot transfer stock into the same batch.");

        if (sourceBatch.MedicineId != destBatch.MedicineId)
            throw new InvalidOperationException("Cannot transfer stock between different medicines.");

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

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new ConflictException("The batch stock was modified concurrently by another process. Please reload and retry.");
        }
    }

    public async Task AdjustStockAsync(AdjustStockDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var batch = await _batchRepository.GetByIdAsync(dto.BatchId, cancellationToken);

        if (batch == null || batch.TenantId != tenantId)
            throw new NotFoundException($"Batch with ID {dto.BatchId} was not found.");

        if (!_currentUserService.CanAccessBranch(batch.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to access branch {batch.BranchId}.");

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
        
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new ConflictException("The batch stock was modified concurrently by another process. Please reload and retry.");
        }
    }

    public async Task<BatchResponseDto> GetBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var batch = await _batchRepository.GetByIdAsync(batchId, cancellationToken);

        if (batch == null || batch.TenantId != tenantId)
            throw new NotFoundException($"Batch with ID {batchId} was not found.");

        if (!_currentUserService.CanAccessBranch(batch.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to access branch {batch.BranchId}.");

        return _mapper.Map<BatchResponseDto>(batch);
    }

    public async Task<PagedResultDto<StockMovementResponseDto>> GetStockMovementsAsync(
        int batchId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var batch = await _batchRepository.GetByIdAsync(batchId, cancellationToken);

        if (batch == null || batch.TenantId != tenantId)
            throw new NotFoundException($"Batch with ID {batchId} was not found.");

        if (!_currentUserService.CanAccessBranch(batch.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to access branch {batch.BranchId}.");

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 100);

        var (movements, totalCount) = await _stockMovementRepository.GetPagedAsync(
            m => m.BatchId == batchId && m.TenantId == tenantId,
            safePage,
            safePageSize,
            q => q.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id),
            cancellationToken);

        var dtos = _mapper.Map<List<StockMovementResponseDto>>(movements);
        return new PagedResultDto<StockMovementResponseDto>(dtos, totalCount, safePage, safePageSize);
    }

    private static bool IsConcurrencyConflict(Exception ex)
    {
        var current = (Exception?)ex;
        while (current != null)
        {
            if (current.GetType().Name.Equals("DbUpdateConcurrencyException", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            current = current.InnerException;
        }
        return false;
    }
}
