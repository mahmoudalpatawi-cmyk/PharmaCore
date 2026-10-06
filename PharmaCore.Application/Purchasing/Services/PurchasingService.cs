using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Purchasing.DTOs;
using PharmaCore.Application.Purchasing.Interfaces;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchasing;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Purchasing.Services;

public class PurchasingService : IPurchasingService
{
    private readonly IRepository<PurchaseInvoice> _invoiceRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Batch> _batchRepository;
    private readonly IRepository<StockMovement> _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public PurchasingService(
        IRepository<PurchaseInvoice> invoiceRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Batch> batchRepository,
        IRepository<StockMovement> stockMovementRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _invoiceRepository = invoiceRepository;
        _supplierRepository = supplierRepository;
        _batchRepository = batchRepository;
        _stockMovementRepository = stockMovementRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        
        var supplier = new Supplier
        {
            TenantId = tenantId,
            Name = dto.Name,
            Phone = dto.Phone,
            Address = dto.Address
        };

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<PurchaseInvoiceResponseDto> CreatePurchaseInvoiceAsync(CreatePurchaseInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId, cancellationToken);
        if (supplier == null || supplier.TenantId != tenantId)
            throw new UnauthorizedAccessException("Supplier not found or unauthorized.");

        decimal totalAmount = dto.Items.Sum(i => (i.Quantity * i.CostPrice) - i.Discount);

        var invoice = new PurchaseInvoice(
            tenantId,
            dto.InvoiceNumber,
            dto.SupplierId,
            dto.BranchId,
            dto.PaymentMethod,
            totalAmount,
            dto.TaxAmount,
            dto.DiscountAmount,
            dto.PaidAmount
        );

        foreach (var itemDto in dto.Items)
        {
            // Try to find an existing batch
            var existingBatches = await _batchRepository.ListAsync(
                b => b.TenantId == tenantId && 
                     b.BranchId == dto.BranchId && 
                     b.MedicineId == itemDto.MedicineId && 
                     b.BatchNumber == itemDto.BatchNumber, 
                cancellationToken);
                
            var batch = existingBatches.FirstOrDefault();
            
            int quantityBefore = 0;
            
            if (batch == null)
            {
                batch = new Batch(
                    tenantId,
                    dto.BranchId,
                    itemDto.MedicineId,
                    itemDto.BatchNumber,
                    itemDto.ExpiryDate,
                    0, // Initial will be added via AddStock below to trigger movement logically if needed, but we do it manually to record movement properly
                    itemDto.CostPrice,
                    itemDto.SellingPrice,
                    dto.SupplierId
                );
                await _batchRepository.AddAsync(batch, cancellationToken);
            }
            else
            {
                quantityBefore = batch.Quantity;
                batch.CostPrice = itemDto.CostPrice;
                batch.SellingPrice = itemDto.SellingPrice;
            }

            batch.AddStock(itemDto.Quantity);

            var movement = new StockMovement
            {
                TenantId = tenantId,
                BranchId = dto.BranchId,
                MovementType = StockMovementType.Purchase,
                QuantityBefore = quantityBefore,
                QuantityChanged = itemDto.Quantity,
                QuantityAfter = batch.Quantity,
                Reason = $"Purchase Invoice {invoice.InvoiceNumber}"
            };

            batch.StockMovements.Add(movement);

            var invoiceItem = new PurchaseInvoiceItem
            {
                TenantId = tenantId,
                Batch = batch, 
                Quantity = itemDto.Quantity,
                PurchasePrice = itemDto.CostPrice,
                SellingPrice = itemDto.SellingPrice,
                SubTotal = (itemDto.Quantity * itemDto.CostPrice) - itemDto.Discount
            };
            
            invoice.Items.Add(invoiceItem);
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PurchaseInvoiceResponseDto>(invoice);
    }

    public async Task<PurchaseInvoiceResponseDto> GetPurchaseInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);

        if (invoice == null || invoice.TenantId != tenantId)
            throw new UnauthorizedAccessException("Invoice not found or unauthorized.");

        return _mapper.Map<PurchaseInvoiceResponseDto>(invoice);
    }
}
