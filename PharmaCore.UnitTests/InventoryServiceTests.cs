using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaCore.Application.Common.Exceptions;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Application.Inventory.DTOs;
using PharmaCore.Application.Inventory.Mappings;
using PharmaCore.Application.Inventory.Services;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Enums;
using PharmaCore.Infrastructure.Data;
using PharmaCore.Infrastructure.Repositories;
using Xunit;

namespace PharmaCore.UnitTests;

public class InventoryServiceTests
{
    private class TestFailingUnitOfWork : IUnitOfWork
    {
        private readonly Exception _exceptionToThrow;
        public TestFailingUnitOfWork(Exception exceptionToThrow)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw _exceptionToThrow;
        }
    }
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

    public InventoryServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<InventoryMappingProfile>();
        });
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();
    }

    private (ApplicationDbContext db, UnitOfWork uow, TestTenantProvider tp) CreateContext(string dbName)
    {
        var tp = new TestTenantProvider { TenantId = _tenantId };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ApplicationDbContext(options, tp);
        var uow = new UnitOfWork(db);
        return (db, uow, tp);
    }

    // ─── ISSUE A: Correct Missing-Resource Handling (NotFoundException vs 403) ───

    [Fact]
    public async Task GetBatchAsync_NonexistentBatch_ThrowsNotFoundException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetBatchAsync(9999));
        Assert.Contains("9999", ex.Message);
    }

    [Fact]
    public async Task GetBatchAsync_BatchBelongingToAnotherTenant_ThrowsNotFoundException_WithoutLeakingExistence()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var otherTenantId = Guid.NewGuid();
        var batch = new Batch(otherTenantId, 1, 10, "B-OTHER", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetBatchAsync(batch.Id));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBatchAsync_ExistingBatchInSameTenant_UnauthorizedBranch_ThrowsUnauthorizedAccessException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var batch = new Batch(_tenantId, 2, 10, "B-BRANCH2", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        // User is bound to Branch 1, cannot access Branch 2
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetBatchAsync(batch.Id));
    }

    [Fact]
    public async Task GetStockMovementsAsync_NonexistentBatch_ThrowsNotFoundException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetStockMovementsAsync(8888));
    }

    [Fact]
    public async Task AdjustStockAsync_NonexistentBatch_ThrowsNotFoundException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new AdjustStockDto { BatchId = 7777, NewQuantity = 10, Reason = "Correction" };
        await Assert.ThrowsAsync<NotFoundException>(() => service.AdjustStockAsync(dto));
    }

    [Fact]
    public async Task TransferStockAsync_NonexistentSourceBatch_ThrowsNotFoundException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var destBatch = new Batch(_tenantId, 2, 10, "B-DEST", DateTime.UtcNow.AddYears(1), 20, 5m, 10m);
        db.Batches.Add(destBatch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = null, Role = "Admin" };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = 9999,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Quantity = 5
        };

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.TransferStockAsync(dto));
        Assert.Contains("Source batch", ex.Message);
    }

    [Fact]
    public async Task TransferStockAsync_NonexistentDestinationBatch_ThrowsNotFoundException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var sourceBatch = new Batch(_tenantId, 1, 10, "B-SRC", DateTime.UtcNow.AddYears(1), 20, 5m, 10m);
        db.Batches.Add(sourceBatch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = null, Role = "Admin" };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = 9999,
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Quantity = 5
        };

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.TransferStockAsync(dto));
        Assert.Contains("Destination batch", ex.Message);
    }

    // ─── ISSUE B: Cross-Medicine Transfer Prevention & Integrity ─────────────────

    [Fact]
    public async Task TransferStockAsync_SameMedicineDifferentBranches_SucceedsAndRecordsLedger()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var sourceBatch = new Batch(_tenantId, 1, 10, "B-SRC", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        var destBatch = new Batch(_tenantId, 2, 10, "B-DST", DateTime.UtcNow.AddYears(1), 20, 5m, 10m);
        db.Batches.AddRange(sourceBatch, destBatch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = null, Role = "Owner" };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Quantity = 15,
            Reason = "Branch rebalance"
        };

        await service.TransferStockAsync(dto);

        var reloadedSource = await db.Batches.FindAsync(sourceBatch.Id);
        var reloadedDest = await db.Batches.FindAsync(destBatch.Id);
        Assert.Equal(35, reloadedSource!.Quantity);
        Assert.Equal(35, reloadedDest!.Quantity);

        var movements = await db.StockMovements.ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Contains(movements, m => m.MovementType == StockMovementType.TransferOut && m.QuantityChanged == -15);
        Assert.Contains(movements, m => m.MovementType == StockMovementType.TransferIn && m.QuantityChanged == 15);
    }

    [Fact]
    public async Task TransferStockAsync_DifferentMedicines_ThrowsInvalidOperationException_AndMutatesNothing()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        // Source is Medicine 10 (e.g. Paracetamol), Dest is Medicine 20 (e.g. Insulin)
        var sourceBatch = new Batch(_tenantId, 1, 10, "B-PARA", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        var destBatch = new Batch(_tenantId, 2, 20, "B-INSU", DateTime.UtcNow.AddYears(1), 20, 50m, 70m);
        db.Batches.AddRange(sourceBatch, destBatch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = null, Role = "Admin" };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Quantity = 10
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransferStockAsync(dto));
        Assert.Contains("different medicines", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify state is completely unchanged
        var reloadedSource = await db.Batches.FindAsync(sourceBatch.Id);
        var reloadedDest = await db.Batches.FindAsync(destBatch.Id);
        Assert.Equal(50, reloadedSource!.Quantity);
        Assert.Equal(20, reloadedDest!.Quantity);
        Assert.Equal(0, await db.StockMovements.CountAsync());
    }

    [Fact]
    public async Task TransferStockAsync_InsufficientQuantity_ThrowsInvalidOperationException_AndDoesNotMutateStock()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var sourceBatch = new Batch(_tenantId, 1, 10, "B-SRC", DateTime.UtcNow.AddYears(1), 10, 5m, 10m);
        var destBatch = new Batch(_tenantId, 2, 10, "B-DST", DateTime.UtcNow.AddYears(1), 20, 5m, 10m);
        db.Batches.AddRange(sourceBatch, destBatch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = null, Role = "Admin" };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Quantity = 50 // exceeds available 10
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransferStockAsync(dto));

        var reloadedSource = await db.Batches.FindAsync(sourceBatch.Id);
        var reloadedDest = await db.Batches.FindAsync(destBatch.Id);
        Assert.Equal(10, reloadedSource!.Quantity);
        Assert.Equal(20, reloadedDest!.Quantity);
        Assert.Equal(0, await db.StockMovements.CountAsync());
    }

    [Fact]
    public async Task TransferStockAsync_SameBranch_ThrowsInvalidOperationException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var sourceBatch = new Batch(_tenantId, 1, 10, "B-1", DateTime.UtcNow.AddYears(1), 10, 5m, 10m);
        var destBatch = new Batch(_tenantId, 1, 10, "B-2", DateTime.UtcNow.AddYears(1), 20, 5m, 10m);
        db.Batches.AddRange(sourceBatch, destBatch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 1,
            DestinationBranchId = 1,
            Quantity = 5
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransferStockAsync(dto));
        Assert.Contains("different branches", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ─── ISSUE C: Validate MedicineId in AddBatchAsync ──────────────────────────

    [Fact]
    public async Task AddBatchAsync_ValidMedicineInCurrentTenant_Succeeds()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var medicine = new Medicine
        {
            TenantId = _tenantId,
            Name = "Amoxicillin 500mg",
            DosageForm = "Capsule",
            Strength = "500mg"
        };
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new AddBatchDto
        {
            BranchId = 1,
            MedicineId = medicine.Id,
            BatchNumber = "LOT-12345",
            ExpiryDate = DateTime.UtcNow.AddMonths(12),
            InitialQuantity = 100,
            CostPrice = 4.5m,
            SellingPrice = 8m
        };

        var result = await service.AddBatchAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("LOT-12345", result.BatchNumber);
        Assert.Equal(100, result.Quantity);
        Assert.Equal(1, await db.Batches.CountAsync());
    }

    [Fact]
    public async Task AddBatchAsync_NonexistentMedicine_ThrowsNotFoundException_AndDoesNotPersist()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new AddBatchDto
        {
            BranchId = 1,
            MedicineId = 9999,
            BatchNumber = "LOT-FAIL",
            ExpiryDate = DateTime.UtcNow.AddMonths(12),
            InitialQuantity = 50,
            CostPrice = 5m,
            SellingPrice = 10m
        };

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.AddBatchAsync(dto));
        Assert.Contains("9999", ex.Message);
        Assert.Equal(0, await db.Batches.CountAsync());
    }

    [Fact]
    public async Task AddBatchAsync_MedicineBelongingToAnotherTenant_ThrowsNotFoundException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var otherTenantId = Guid.NewGuid();
        var medicine = new Medicine
        {
            TenantId = otherTenantId,
            Name = "Other Tenant Drug",
            DosageForm = "Tablet",
            Strength = "10mg"
        };
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new AddBatchDto
        {
            BranchId = 1,
            MedicineId = medicine.Id,
            BatchNumber = "LOT-FAIL-TENANT",
            ExpiryDate = DateTime.UtcNow.AddMonths(12),
            InitialQuantity = 50,
            CostPrice = 5m,
            SellingPrice = 10m
        };

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.AddBatchAsync(dto));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await db.Batches.CountAsync());
    }

    [Fact]
    public async Task AddBatchAsync_SoftDeletedMedicine_ThrowsNotFoundException()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var medicine = new Medicine
        {
            TenantId = _tenantId,
            Name = "Deleted Medicine",
            DosageForm = "Liquid",
            Strength = "100ml",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        var dto = new AddBatchDto
        {
            BranchId = 1,
            MedicineId = medicine.Id,
            BatchNumber = "LOT-FAIL-DELETED",
            ExpiryDate = DateTime.UtcNow.AddMonths(12),
            InitialQuantity = 50,
            CostPrice = 5m,
            SellingPrice = 10m
        };

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddBatchAsync(dto));
        Assert.Equal(0, await db.Batches.CountAsync());
    }

    // ─── ISSUE D: Concurrency Handling & Domain Rules ──────────────────────────

    [Fact]
    public async Task TransferStockAsync_ConcurrencyConflictOnSave_ThrowsConflictException()
    {
        var (db, _, tp) = CreateContext(Guid.NewGuid().ToString());
        var sourceBatch = new Batch(_tenantId, 1, 10, "B-SRC", DateTime.UtcNow.AddYears(1), 50, 5m, 10m);
        var destBatch = new Batch(_tenantId, 2, 10, "B-DST", DateTime.UtcNow.AddYears(1), 20, 5m, 10m);
        db.Batches.AddRange(sourceBatch, destBatch);
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = null, Role = "Admin" };
        var failingUow = new TestFailingUnitOfWork(new DbUpdateConcurrencyException("Concurrency conflict simulating race condition."));

        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, failingUow, tp, currentUser, _mapper);

        var dto = new StockTransferDto
        {
            SourceBatchId = sourceBatch.Id,
            DestinationBatchId = destBatch.Id,
            SourceBranchId = 1,
            DestinationBranchId = 2,
            Quantity = 10
        };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.TransferStockAsync(dto));
        Assert.Contains("concurrently", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Batch_DomainQuantityRules_EnforcesInvariants()
    {
        var batch = new Batch(_tenantId, 1, 10, "B-TEST", DateTime.UtcNow.AddYears(1), 100, 5m, 10m);

        batch.DeductStock(40);
        Assert.Equal(60, batch.Quantity);

        batch.AddStock(25);
        Assert.Equal(85, batch.Quantity);

        batch.AdjustStock(50);
        Assert.Equal(50, batch.Quantity);

        Assert.Throws<InvalidOperationException>(() => batch.DeductStock(51));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.DeductStock(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.AddStock(-5));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.AdjustStock(-1));
    }

    // ─── PHASE 3: Paged Movements & Query Safety ───────────────────────────────

    [Fact]
    public async Task GetStockMovementsAsync_ReturnsBoundedDeterministicPagedResult()
    {
        var (db, uow, tp) = CreateContext(Guid.NewGuid().ToString());
        var batch = new Batch(_tenantId, 1, 10, "B-PAGE", DateTime.UtcNow.AddYears(1), 100, 5m, 10m);
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        for (int i = 1; i <= 60; i++)
        {
            db.StockMovements.Add(new StockMovement
            {
                TenantId = _tenantId,
                BranchId = 1,
                BatchId = batch.Id,
                MovementType = StockMovementType.Adjustment,
                QuantityBefore = i - 1,
                QuantityChanged = 1,
                QuantityAfter = i,
                Reason = $"Audit entry {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        var batchRepo = new GenericRepository<Batch>(db);
        var movementRepo = new GenericRepository<StockMovement>(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var currentUser = new TestCurrentUserService { BranchId = 1 };
        var service = new InventoryService(batchRepo, movementRepo, medicineRepo, uow, tp, currentUser, _mapper);

        // Request page 1 with default page size 50
        var page1 = await service.GetStockMovementsAsync(batch.Id, page: 1, pageSize: 50);

        Assert.NotNull(page1);
        Assert.Equal(60, page1.TotalCount);
        Assert.Equal(1, page1.Page);
        Assert.Equal(50, page1.PageSize);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(50, page1.Items.Count);
        Assert.True(page1.HasNextPage);
        Assert.False(page1.HasPreviousPage);

        // Check page size clamping (max 100)
        var clamped = await service.GetStockMovementsAsync(batch.Id, page: 1, pageSize: 500);
        Assert.Equal(100, clamped.PageSize);
    }
}
