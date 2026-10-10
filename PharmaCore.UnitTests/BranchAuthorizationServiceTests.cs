using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaCore.Application.Finance.DTOs;
using PharmaCore.Application.Finance.Mappings;
using PharmaCore.Application.Finance.Services;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Application.Inventory.DTOs;
using PharmaCore.Application.Inventory.Mappings;
using PharmaCore.Application.Inventory.Services;
using PharmaCore.Application.Purchasing.DTOs;
using PharmaCore.Application.Purchasing.Mappings;
using PharmaCore.Application.Purchasing.Services;
using PharmaCore.Application.Sales.DTOs;
using PharmaCore.Application.Sales.Mappings;
using PharmaCore.Application.Sales.Services;
using PharmaCore.Application.Transfers.DTOs;
using PharmaCore.Application.Transfers.Mappings;
using PharmaCore.Application.Transfers.Services;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchasing;
using PharmaCore.Domain.Entities.Sales;
using PharmaCore.Domain.Entities.Shifts;
using PharmaCore.Domain.Enums;
using PharmaCore.Infrastructure.Data;
using PharmaCore.Infrastructure.Repositories;
using Xunit;

namespace PharmaCore.UnitTests;

public class BranchAuthorizationServiceTests
{
    private class TestTenantProvider : ITenantProvider
    {
        public Guid TenantId { get; set; } = Guid.NewGuid();
        public Guid GetTenantId() => TenantId;
    }

    private class TestCurrentUserService : ICurrentUserService
    {
        public int? UserId { get; set; } = 1;
        public Guid? TenantId { get; set; }
        public int? BranchId { get; set; }
        public string? Role { get; set; }
        public bool IsAuthenticated { get; set; } = true;
        public Func<int, bool>? CanAccessBranchFunc { get; set; }

        public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
        public bool CanAccessBranch(int targetBranchId) =>
            CanAccessBranchFunc != null ? CanAccessBranchFunc(targetBranchId) : (BranchId == null || BranchId == targetBranchId);
    }

    private readonly IMapper _mapper;
    private readonly Guid _tenantId = Guid.NewGuid();

    public BranchAuthorizationServiceTests()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<InventoryMappingProfile>();
            cfg.AddProfile<SalesMappingProfile>();
            cfg.AddProfile<PurchasingMappingProfile>();
            cfg.AddProfile<FinanceMappingProfile>();
            cfg.AddProfile<TransferMappingProfile>();
        });
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();
    }

    private (ApplicationDbContext db, UnitOfWork uow, TestTenantProvider tenantProvider) CreateContext(string dbName)
    {
        var tenantProvider = new TestTenantProvider { TenantId = _tenantId };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ApplicationDbContext(options, tenantProvider);
        var uow = new UnitOfWork(db);
        return (db, uow, tenantProvider);
    }

    // ─── 1. InventoryService Branch Authorization Tests ────────────────────────

    [Fact]
    public async Task AddBatchAsync_UnauthorizedBranch_ThrowsUnauthorizedAccessException_AndDoesNotPersist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);

        // User is bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        // Attempting to add batch to Branch 2
        var dto = new AddBatchDto
        {
            BranchId = 2,
            MedicineId = 10,
            BatchNumber = "BATCH-999",
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            InitialQuantity = 50,
            CostPrice = 10m,
            SellingPrice = 15m
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AddBatchAsync(dto));
        Assert.Contains("branch 2", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify NO batch or movement was persisted
        Assert.Equal(0, await db.Batches.CountAsync());
        Assert.Equal(0, await db.StockMovements.CountAsync());
    }

    [Fact]
    public async Task AddBatchAsync_AuthorizedBranch_PersistsSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);

        var medicine = new Medicine
        {
            Id = 10,
            TenantId = _tenantId,
            Name = "Amoxicillin",
            DosageForm = "Capsule",
            Strength = "500mg"
        };
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();

        // User is authorized for Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new AddBatchDto
        {
            BranchId = 1,
            MedicineId = 10,
            BatchNumber = "BATCH-001",
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            InitialQuantity = 20,
            CostPrice = 5m,
            SellingPrice = 10m
        };

        var result = await service.AddBatchAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(1, await db.Batches.CountAsync());
    }

    [Fact]
    public async Task TransferStockAsync_UnauthorizedSourceBranch_ThrowsUnauthorizedAccessException_AndDoesNotMutateStock()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);

        var sourceBatch = new Batch(_tenantId, 2, 10, "B-SRC", DateTime.UtcNow.AddYears(1), 100, 5m, 10m);
        var destBatch = new Batch(_tenantId, 1, 10, "B-DST", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        db.Batches.AddRange(sourceBatch, destBatch);
        await db.SaveChangesAsync();

        // User is bound to Branch 1 (has destination, lacks source branch 2)
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 2,
            DestinationBranchId = 1,
            Quantity = 20
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.TransferStockAsync(dto));

        // Verify stock was not deducted or added
        var reloadedSource = await db.Batches.FindAsync(sourceBatch.Id);
        var reloadedDest = await db.Batches.FindAsync(destBatch.Id);
        Assert.Equal(100, reloadedSource!.Quantity);
        Assert.Equal(50, reloadedDest!.Quantity);
    }

    [Fact]
    public async Task TransferStockAsync_UnauthorizedDestinationBranch_ThrowsUnauthorizedAccessException_AndDoesNotMutateStock()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);

        var sourceBatch = new Batch(_tenantId, 1, 10, "B-SRC", DateTime.UtcNow.AddYears(1), 100, 5m, 10m);
        var destBatch = new Batch(_tenantId, 2, 10, "B-DST", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        db.Batches.AddRange(sourceBatch, destBatch);
        await db.SaveChangesAsync();

        // User is bound to Branch 1 (has source, lacks destination branch 2)
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Quantity = 20
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.TransferStockAsync(dto));

        var reloadedSource = await db.Batches.FindAsync(sourceBatch.Id);
        var reloadedDest = await db.Batches.FindAsync(destBatch.Id);
        Assert.Equal(100, reloadedSource!.Quantity);
        Assert.Equal(50, reloadedDest!.Quantity);
    }

    [Fact]
    public async Task AdjustStockAsync_UnauthorizedBatchBranch_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);

        var batch = new Batch(_tenantId, 2, 10, "B-ADJ", DateTime.UtcNow.AddYears(1), 100, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new AdjustStockDto
        {
            BatchId = batch.Id,
            NewQuantity = 80,
            Reason = "Damaged items"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AdjustStockAsync(dto));

        var reloaded = await db.Batches.FindAsync(batch.Id);
        Assert.Equal(100, reloaded!.Quantity);
    }

    [Fact]
    public async Task GetBatchAsync_UnauthorizedBatchBranch_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);

        var batch = new Batch(_tenantId, 5, 10, "B-5", DateTime.UtcNow.AddYears(1), 10, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetBatchAsync(batch.Id));
    }

    // ─── 2. SalesService Branch Authorization Tests ────────────────────────────

    [Fact]
    public async Task CreateSaleInvoiceAsync_UnauthorizedBranch_ThrowsUnauthorizedAccessException_AndDoesNotDeductStock()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var invoiceRepo = new GenericRepository<SaleInvoice>(db);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var customerRepo = new GenericRepository<Customer>(db);

        var batch = new Batch(_tenantId, 2, 10, "B-SALE", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new SalesService(invoiceRepo, batchRepo, movementRepo, customerRepo, uow, tp, currentUser, _mapper);

        var dto = new CreateSaleInvoiceDto
        {
            BranchId = 2,
            PaymentMethod = PaymentMethod.Cash,
            PaidAmount = 20m,
            Items = new List<SaleInvoiceItemDto>
            {
                new SaleInvoiceItemDto { BatchId = batch.Id, Quantity = 2, UnitPrice = 10m }
            }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateSaleInvoiceAsync(dto));

        var reloadedBatch = await db.Batches.FindAsync(batch.Id);
        Assert.Equal(50, reloadedBatch!.Quantity);
        Assert.Equal(0, await db.SaleInvoices.CountAsync());
    }

    [Fact]
    public async Task CreateSaleInvoiceAsync_BatchFromUnauthorizedBranch_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var invoiceRepo = new GenericRepository<SaleInvoice>(db);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var customerRepo = new GenericRepository<Customer>(db);

        // Batch belongs to Branch 2
        var batch = new Batch(_tenantId, 2, 10, "B-SALE", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        // User bound to Branch 1, but calls with Branch 1 using Branch 2's batch
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new SalesService(invoiceRepo, batchRepo, movementRepo, customerRepo, uow, tp, currentUser, _mapper);

        var dto = new CreateSaleInvoiceDto
        {
            BranchId = 1,
            PaymentMethod = PaymentMethod.Cash,
            PaidAmount = 20m,
            Items = new List<SaleInvoiceItemDto>
            {
                new SaleInvoiceItemDto { BatchId = batch.Id, Quantity = 2, UnitPrice = 10m }
            }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateSaleInvoiceAsync(dto));
        Assert.Equal(50, (await db.Batches.FindAsync(batch.Id))!.Quantity);
    }

    [Fact]
    public async Task CreateSaleInvoiceAsync_AuthorizedBranch_PersistsSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var invoiceRepo = new GenericRepository<SaleInvoice>(db);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var customerRepo = new GenericRepository<Customer>(db);

        var batch = new Batch(_tenantId, 1, 10, "B-SALE-1", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new SalesService(invoiceRepo, batchRepo, movementRepo, customerRepo, uow, tp, currentUser, _mapper);

        var dto = new CreateSaleInvoiceDto
        {
            BranchId = 1,
            PaymentMethod = PaymentMethod.Cash,
            PaidAmount = 20m,
            Items = new List<SaleInvoiceItemDto>
            {
                new SaleInvoiceItemDto { BatchId = batch.Id, Quantity = 2, UnitPrice = 10m }
            }
        };

        var result = await service.CreateSaleInvoiceAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(1, await db.SaleInvoices.CountAsync());
        Assert.Equal(48, (await db.Batches.FindAsync(batch.Id))!.Quantity);
    }

    [Fact]
    public async Task GetSaleInvoiceByIdAsync_UnauthorizedBranch_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var invoiceRepo = new GenericRepository<SaleInvoice>(db);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var customerRepo = new GenericRepository<Customer>(db);

        var invoice = new SaleInvoice(_tenantId, "INV-001", 3, 1, PaymentMethod.Cash, 100m, 0m, 0m, 100m, null, null, InvoiceType.Sale);
        db.SaleInvoices.Add(invoice);
        await db.SaveChangesAsync();

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new SalesService(invoiceRepo, batchRepo, movementRepo, customerRepo, uow, tp, currentUser, _mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSaleInvoiceByIdAsync(invoice.Id));
    }

    // ─── 3. PurchasingService Branch Authorization Tests ───────────────────────

    [Fact]
    public async Task CreatePurchaseInvoiceAsync_UnauthorizedBranch_ThrowsUnauthorizedAccessException_AndDoesNotPersist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var invoiceRepo = new GenericRepository<PurchaseInvoice>(db);
        var supplierRepo = new GenericRepository<Supplier>(db);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);

        var supplier = new Supplier { TenantId = _tenantId, Name = "Test Supplier" };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new PurchasingService(invoiceRepo, supplierRepo, batchRepo, movementRepo, uow, tp, currentUser, _mapper);

        var dto = new CreatePurchaseInvoiceDto
        {
            BranchId = 2, // Unauthorized
            SupplierId = supplier.Id,
            InvoiceNumber = "PINV-001",
            PaymentMethod = PaymentMethod.Cash,
            Items = new List<PurchaseInvoiceItemDto>
            {
                new PurchaseInvoiceItemDto { MedicineId = 1, BatchNumber = "B-P1", CostPrice = 10m, SellingPrice = 15m, Quantity = 10, ExpiryDate = DateTime.UtcNow.AddMonths(12) }
            }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreatePurchaseInvoiceAsync(dto));
        Assert.Equal(0, await db.PurchaseInvoices.CountAsync());
    }

    [Fact]
    public async Task CreatePurchaseInvoiceAsync_AuthorizedBranch_PersistsSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var invoiceRepo = new GenericRepository<PurchaseInvoice>(db);
        var supplierRepo = new GenericRepository<Supplier>(db);
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);

        var supplier = new Supplier { TenantId = _tenantId, Name = "Test Supplier" };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new PurchasingService(invoiceRepo, supplierRepo, batchRepo, movementRepo, uow, tp, currentUser, _mapper);

        var dto = new CreatePurchaseInvoiceDto
        {
            BranchId = 1,
            SupplierId = supplier.Id,
            InvoiceNumber = "PINV-001",
            PaymentMethod = PaymentMethod.Cash,
            Items = new List<PurchaseInvoiceItemDto>
            {
                new PurchaseInvoiceItemDto { MedicineId = 1, BatchNumber = "B-P1", CostPrice = 10m, SellingPrice = 15m, Quantity = 10, ExpiryDate = DateTime.UtcNow.AddMonths(12) }
            }
        };

        var result = await service.CreatePurchaseInvoiceAsync(dto);
        Assert.NotNull(result);
        Assert.Equal(1, await db.PurchaseInvoices.CountAsync());
    }

    // ─── 4. FinanceService Branch Authorization Tests ──────────────────────────

    [Fact]
    public async Task OpenShiftAsync_UnauthorizedBranch_ThrowsUnauthorizedAccessException_AndDoesNotPersist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var shiftRepo = new GenericRepository<Shift>(db);
        var cashRepo = new GenericRepository<CashTransaction>(db);
        var saleRepo = new GenericRepository<SaleInvoice>(db);

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new FinanceService(shiftRepo, cashRepo, saleRepo, uow, tp, currentUser, _mapper);

        var dto = new OpenShiftDto
        {
            BranchId = 2, // Unauthorized
            StartingCash = 100m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.OpenShiftAsync(dto));
        Assert.Equal(0, await db.Shifts.CountAsync());
    }

    [Fact]
    public async Task OpenShiftAsync_AuthorizedBranch_PersistsSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var shiftRepo = new GenericRepository<Shift>(db);
        var cashRepo = new GenericRepository<CashTransaction>(db);
        var saleRepo = new GenericRepository<SaleInvoice>(db);

        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new FinanceService(shiftRepo, cashRepo, saleRepo, uow, tp, currentUser, _mapper);

        var dto = new OpenShiftDto
        {
            BranchId = 1,
            StartingCash = 100m
        };

        var result = await service.OpenShiftAsync(dto);
        Assert.NotNull(result);
        Assert.Equal(1, await db.Shifts.CountAsync());
    }

    [Fact]
    public async Task CloseShiftAsync_UnauthorizedShiftBranch_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var shiftRepo = new GenericRepository<Shift>(db);
        var cashRepo = new GenericRepository<CashTransaction>(db);
        var saleRepo = new GenericRepository<SaleInvoice>(db);

        var shift = new Shift { TenantId = _tenantId, BranchId = 3, Status = ShiftStatus.Open, OpeningCash = 50m };
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new FinanceService(shiftRepo, cashRepo, saleRepo, uow, tp, currentUser, _mapper);

        var dto = new CloseShiftDto { ShiftId = shift.Id, EndingCash = 50m };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CloseShiftAsync(dto));

        var reloaded = await db.Shifts.FindAsync(shift.Id);
        Assert.Equal(ShiftStatus.Open, reloaded!.Status);
    }

    [Fact]
    public async Task AddCashTransactionAsync_UnauthorizedShiftBranch_ThrowsUnauthorizedAccessException_AndDoesNotPersist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var shiftRepo = new GenericRepository<Shift>(db);
        var cashRepo = new GenericRepository<CashTransaction>(db);
        var saleRepo = new GenericRepository<SaleInvoice>(db);

        var shift = new Shift { TenantId = _tenantId, BranchId = 4, Status = ShiftStatus.Open, OpeningCash = 50m };
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();

        // User bound to Branch 1
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new FinanceService(shiftRepo, cashRepo, saleRepo, uow, tp, currentUser, _mapper);

        var dto = new CashTransactionDto
        {
            ShiftId = shift.Id,
            Amount = 15m,
            TransactionType = TransactionType.Expense,
            Category = "Supplies"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AddCashTransactionAsync(dto));
        Assert.Equal(0, await db.CashTransactions.CountAsync());
    }

    // ─── 5. TransferDocumentService Branch Authorization Tests ─────────────────

    [Fact]
    public async Task CreateTransferDocumentAsync_UnauthorizedSourceBranch_ThrowsUnauthorizedAccessException_AndDoesNotPersist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var transferRepo = new GenericRepository<StockTransfer>(db);
        var itemRepo = new GenericRepository<StockTransferItem>(db);

        // User bound to Branch 1 (has dest, lacks source 2)
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new TransferDocumentService(transferRepo, itemRepo, uow, tp, currentUser, _mapper);

        var dto = new CreateTransferDocumentDto
        {
            SourceBranchId = 2,
            DestinationBranchId = 1,
            Items = new List<TransferDocumentItemDto> { new TransferDocumentItemDto { BatchId = 1, Quantity = 5 } }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateTransferDocumentAsync(dto));
        Assert.Equal(0, await db.StockTransfers.CountAsync());
    }

    [Fact]
    public async Task CreateTransferDocumentAsync_UnauthorizedDestinationBranch_ThrowsUnauthorizedAccessException_AndDoesNotPersist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var transferRepo = new GenericRepository<StockTransfer>(db);
        var itemRepo = new GenericRepository<StockTransferItem>(db);

        // User bound to Branch 1 (has source, lacks dest 2)
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new TransferDocumentService(transferRepo, itemRepo, uow, tp, currentUser, _mapper);

        var dto = new CreateTransferDocumentDto
        {
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Items = new List<TransferDocumentItemDto> { new TransferDocumentItemDto { BatchId = 1, Quantity = 5 } }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateTransferDocumentAsync(dto));
        Assert.Equal(0, await db.StockTransfers.CountAsync());
    }

    [Fact]
    public async Task CreateTransferDocumentAsync_OwnerWithNullBranch_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var transferRepo = new GenericRepository<StockTransfer>(db);
        var itemRepo = new GenericRepository<StockTransferItem>(db);

        // Owner with BranchId == null has tenant-wide access
        var currentUser = new TestCurrentUserService { BranchId = null, Role = "Owner" };
        var service = new TransferDocumentService(transferRepo, itemRepo, uow, tp, currentUser, _mapper);

        var dto = new CreateTransferDocumentDto
        {
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Items = new List<TransferDocumentItemDto> { new TransferDocumentItemDto { BatchId = 1, Quantity = 5 } }
        };

        var result = await service.CreateTransferDocumentAsync(dto);
        Assert.NotNull(result);
        Assert.Equal(1, await db.StockTransfers.CountAsync());
    }

    [Fact]
    public async Task UpdateTransferStatusAsync_UserLacksBothBranches_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        var (db, uow, tp) = CreateContext(dbName);
        var transferRepo = new GenericRepository<StockTransfer>(db);
        var itemRepo = new GenericRepository<StockTransferItem>(db);

        var transfer = new StockTransfer { TenantId = _tenantId, FromBranchId = 2, ToBranchId = 3, Status = StockTransferStatus.Pending };
        db.StockTransfers.Add(transfer);
        await db.SaveChangesAsync();

        // User bound to Branch 1 (neither source 2 nor dest 3)
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new TransferDocumentService(transferRepo, itemRepo, uow, tp, currentUser, _mapper);

        var dto = new UpdateTransferStatusDto
        {
            TransferDocumentId = transfer.Id,
            NewStatus = StockTransferStatus.InTransit
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateTransferStatusAsync(dto));
    }
}
