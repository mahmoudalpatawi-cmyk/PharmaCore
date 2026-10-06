using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Transfers.DTOs;
using PharmaCore.Application.Transfers.Interfaces;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Transfers.Services;

public class TransferDocumentService : ITransferDocumentService
{
    private readonly IRepository<StockTransfer> _transferRepository;
    private readonly IRepository<StockTransferItem> _transferItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public TransferDocumentService(
        IRepository<StockTransfer> transferRepository,
        IRepository<StockTransferItem> transferItemRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _transferRepository = transferRepository;
        _transferItemRepository = transferItemRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
    }

    public async Task<TransferDocumentResponseDto> CreateTransferDocumentAsync(CreateTransferDocumentDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var transfer = new StockTransfer
        {
            TenantId = tenantId,
            FromBranchId = dto.SourceBranchId,
            ToBranchId = dto.DestinationBranchId,
            Status = StockTransferStatus.Pending,
            TransferDate = DateTime.UtcNow,
            Notes = dto.Notes
        };

        foreach (var itemDto in dto.Items)
        {
            transfer.Items.Add(new StockTransferItem
            {
                TenantId = tenantId,
                BatchId = itemDto.BatchId,
                Quantity = itemDto.Quantity
            });
        }

        await _transferRepository.AddAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TransferDocumentResponseDto>(transfer);
    }

    public async Task<TransferDocumentResponseDto> UpdateTransferStatusAsync(UpdateTransferStatusDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var transfer = await _transferRepository.GetByIdAsync(dto.TransferDocumentId, cancellationToken);
        
        if (transfer == null || transfer.TenantId != tenantId)
            throw new UnauthorizedAccessException("Transfer document not found or unauthorized.");

        // Workflow Validation
        bool isValidTransition = false;
        
        if (transfer.Status == StockTransferStatus.Pending && dto.NewStatus == StockTransferStatus.InTransit)
            isValidTransition = true;
        else if (transfer.Status == StockTransferStatus.Pending && dto.NewStatus == StockTransferStatus.Cancelled)
            isValidTransition = true;
        else if (transfer.Status == StockTransferStatus.InTransit && dto.NewStatus == StockTransferStatus.Completed)
            isValidTransition = true;
        else if (transfer.Status == StockTransferStatus.InTransit && dto.NewStatus == StockTransferStatus.Cancelled)
            isValidTransition = true;

        if (!isValidTransition)
            throw new InvalidOperationException($"Cannot transition transfer status from {transfer.Status} to {dto.NewStatus}.");

        if (dto.NewStatus == StockTransferStatus.Cancelled && string.IsNullOrWhiteSpace(dto.RejectionReason))
            throw new InvalidOperationException("Rejection/Cancellation reason is required.");

        transfer.Status = dto.NewStatus;

        if (dto.NewStatus == StockTransferStatus.Cancelled)
        {
            transfer.Notes = string.IsNullOrWhiteSpace(transfer.Notes) 
                ? $"Cancelled: {dto.RejectionReason}" 
                : $"{transfer.Notes} | Cancelled: {dto.RejectionReason}";
        }

        await _transferRepository.UpdateAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TransferDocumentResponseDto>(transfer);
    }

    public async Task<TransferDocumentResponseDto> GetTransferDocumentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        
        var transfer = await _transferRepository.GetByIdAsync(id, cancellationToken);
        
        if (transfer == null || transfer.TenantId != tenantId)
            throw new UnauthorizedAccessException("Transfer document not found or unauthorized.");

        if (transfer.Items == null || !transfer.Items.Any())
        {
            var items = await _transferItemRepository.ListAsync(i => i.TransferId == transfer.Id && i.TenantId == tenantId, cancellationToken);
            transfer.Items = items.ToList();
        }

        return _mapper.Map<TransferDocumentResponseDto>(transfer);
    }

    public async Task<IEnumerable<TransferDocumentResponseDto>> GetTransferDocumentsByBranchAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var transfers = await _transferRepository.ListAsync(
            t => t.TenantId == tenantId && (t.FromBranchId == branchId || t.ToBranchId == branchId), 
            cancellationToken);

        foreach (var transfer in transfers)
        {
            if (transfer.Items == null || !transfer.Items.Any())
            {
                var items = await _transferItemRepository.ListAsync(i => i.TransferId == transfer.Id && i.TenantId == tenantId, cancellationToken);
                transfer.Items = items.ToList();
            }
        }

        return _mapper.Map<IEnumerable<TransferDocumentResponseDto>>(transfers);
    }
}
