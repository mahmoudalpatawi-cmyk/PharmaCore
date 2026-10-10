using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Application.Sales.DTOs;
using PharmaCore.Application.Sales.Interfaces;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Sales;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Sales.Services;

public class SalesService : ISalesService
{
    private readonly IRepository<SaleInvoice> _invoiceRepository;
    private readonly IRepository<Batch> _batchRepository;
    private readonly IRepository<StockMovement> _stockMovementRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public SalesService(
        IRepository<SaleInvoice> invoiceRepository,
        IRepository<Batch> batchRepository,
        IRepository<StockMovement> stockMovementRepository,
        IRepository<Customer> customerRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _stockMovementRepository = stockMovementRepository ?? throw new ArgumentNullException(nameof(stockMovementRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<SaleInvoiceResponseDto> CreateSaleInvoiceAsync(CreateSaleInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.CanAccessBranch(dto.BranchId))
        {
            throw new UnauthorizedAccessException($"User is not authorized to create sales for branch {dto.BranchId}.");
        }

        var tenantId = _tenantProvider.GetTenantId();

        int? customerId = null;

        if (!string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            var existingCustomers = await _customerRepository.ListAsync(c => c.TenantId == tenantId && c.Name == dto.CustomerName, cancellationToken);
            var customer = existingCustomers.FirstOrDefault();

            if (customer == null)
            {
                customer = new Customer(tenantId, dto.CustomerName);
                await _customerRepository.AddAsync(customer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            customerId = customer.Id;
        }

        // Aggregate duplicate batches in request (if any) to prevent deducting twice incorrectly in a single loop
        var aggregatedItems = dto.Items
            .GroupBy(i => i.BatchId)
            .Select(g => new 
            {
                BatchId = g.Key,
                Quantity = g.Sum(i => i.Quantity),
                UnitPrice = g.First().UnitPrice,
                Discount = g.Sum(i => i.Discount)
            }).ToList();

        decimal totalAmount = aggregatedItems.Sum(i => i.Quantity * i.UnitPrice);
        decimal discountAmount = aggregatedItems.Sum(i => i.Discount);
        decimal taxAmount = 0; // Assume 0 or calculate if needed
        string invoiceNumber = "INV-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

        var invoice = new SaleInvoice(
            tenantId,
            invoiceNumber,
            dto.BranchId,
            dto.ShiftId,
            dto.PaymentMethod,
            totalAmount,
            taxAmount,
            discountAmount,
            dto.PaidAmount,
            null, // SaleId
            customerId,
            InvoiceType.Sale
        );

        foreach (var item in aggregatedItems)
        {
            var batch = await _batchRepository.GetByIdAsync(item.BatchId, cancellationToken);
            
            if (batch == null || batch.TenantId != tenantId)
                throw new UnauthorizedAccessException($"Batch {item.BatchId} not found or belongs to another tenant.");

            if (!_currentUserService.CanAccessBranch(batch.BranchId))
                throw new UnauthorizedAccessException($"User is not authorized to sell from batch branch {batch.BranchId}.");
                
            if (batch.BranchId != dto.BranchId)
                throw new InvalidOperationException($"Batch {item.BatchId} belongs to a different branch.");

            int quantityBefore = batch.Quantity;
            
            // Domain method guarantees invariants (throws if insufficient stock)
            batch.DeductStock(item.Quantity);

            var movement = new StockMovement
            {
                TenantId = tenantId,
                BranchId = dto.BranchId,
                MovementType = StockMovementType.Sale,
                QuantityBefore = quantityBefore,
                QuantityChanged = -item.Quantity,
                QuantityAfter = batch.Quantity,
                Reason = $"Sale Invoice {invoiceNumber}",
                ReferenceDocumentId = invoiceNumber
            };

            batch.StockMovements.Add(movement);

            var invoiceItem = new SaleInvoiceItem
            {
                TenantId = tenantId,
                Batch = batch,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                SubTotal = (item.Quantity * item.UnitPrice) - item.Discount
            };

            invoice.Items.Add(invoiceItem);
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SaleInvoiceResponseDto>(invoice);
    }

    public async Task<SaleInvoiceResponseDto> GetSaleInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);

        if (invoice == null || invoice.TenantId != tenantId)
            throw new UnauthorizedAccessException("Invoice not found or unauthorized.");

        if (!_currentUserService.CanAccessBranch(invoice.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to access invoice for branch {invoice.BranchId}.");

        return _mapper.Map<SaleInvoiceResponseDto>(invoice);
    }
}
