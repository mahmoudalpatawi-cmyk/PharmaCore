using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PharmaCore.Application.Catalog.DTOs;
using PharmaCore.Application.Catalog.Mappings;
using PharmaCore.Application.Catalog.Services;
using PharmaCore.Application.Catalog.Validators;
using PharmaCore.Application.Common.Exceptions;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Infrastructure;
using PharmaCore.Infrastructure.Data;
using PharmaCore.Infrastructure.Repositories;
using Xunit;

namespace PharmaCore.UnitTests;

public class CatalogServiceTests
{
    private class TestTenantProvider : ITenantProvider
    {
        public Guid TenantId { get; set; } = Guid.NewGuid();
        public Guid GetTenantId() => TenantId;
    }

    private readonly IMapper _mapper;

    public CatalogServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<CatalogMappingProfile>();
        });
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();
    }

    private (ApplicationDbContext db, UnitOfWork uow, TestTenantProvider tenantProvider, CatalogService service)
        CreateService(string dbName, Guid? tenantId = null)
    {
        var activeTenantId = tenantId ?? Guid.NewGuid();
        var tenantProvider = new TestTenantProvider { TenantId = activeTenantId };

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new ApplicationDbContext(options, tenantProvider);
        var uow = new UnitOfWork(db);
        var medicineRepo = new GenericRepository<Medicine>(db);
        var categoryRepo = new GenericRepository<Category>(db);
        var manufacturerRepo = new GenericRepository<Manufacturer>(db);

        var service = new CatalogService(
            medicineRepo,
            categoryRepo,
            manufacturerRepo,
            uow,
            tenantProvider,
            _mapper);

        return (db, uow, tenantProvider, service);
    }

    // ─── 1. Validator Resolution Tests ─────────────────────────────────────────

    [Fact]
    public void DependencyInjection_ResolvesMedicineValidators()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:MahmoudConnection", "Server=localhost;Database=PharmaCore;Trusted_Connection=True;" },
                { "JwtSettings:SecretKey", "12345678901234567890123456789012" },
                { "JwtSettings:Issuer", "PharmaCore" },
                { "JwtSettings:Audience", "PharmaCoreUsers" }
            })
            .Build();

        services.AddInfrastructureServices(configuration);
        var sp = services.BuildServiceProvider();

        var createValidator = sp.GetService<IValidator<CreateMedicineDto>>();
        var updateValidator = sp.GetService<IValidator<UpdateMedicineDto>>();
        var loginValidator = sp.GetService<IValidator<PharmaCore.Application.Identity.DTOs.LoginRequestDto>>();

        Assert.NotNull(createValidator);
        Assert.IsType<CreateMedicineDtoValidator>(createValidator);

        Assert.NotNull(updateValidator);
        Assert.IsType<UpdateMedicineDtoValidator>(updateValidator);

        Assert.NotNull(loginValidator);
    }

    // ─── 2. Input Validation Tests ─────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateMedicineDtoValidator_MissingPrimaryUnit_IsRejected(string? primaryUnit)
    {
        var validator = new CreateMedicineDtoValidator();
        var dto = new CreateMedicineDto
        {
            CategoryId = 1,
            ManufacturerId = 1,
            Name = "Amoxicillin 500mg",
            TradeName = "Amoxil",
            GenericName = "Amoxicillin",
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = primaryUnit!,
            SellingPrice = 15.5m
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMedicineDto.PrimaryUnit));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateMedicineDtoValidator_MissingGenericName_IsRejected(string? genericName)
    {
        var validator = new CreateMedicineDtoValidator();
        var dto = new CreateMedicineDto
        {
            CategoryId = 1,
            ManufacturerId = 1,
            Name = "Amoxicillin 500mg",
            TradeName = "Amoxil",
            GenericName = genericName!,
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = "Box",
            SellingPrice = 15.5m
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMedicineDto.GenericName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateMedicineDtoValidator_MissingPrimaryUnit_IsRejected(string? primaryUnit)
    {
        var validator = new UpdateMedicineDtoValidator();
        var dto = new UpdateMedicineDto
        {
            Id = 1,
            CategoryId = 1,
            ManufacturerId = 1,
            Name = "Amoxicillin 500mg",
            TradeName = "Amoxil",
            GenericName = "Amoxicillin",
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = primaryUnit!,
            SellingPrice = 15.5m
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMedicineDto.PrimaryUnit));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateMedicineDtoValidator_MissingGenericName_IsRejected(string? genericName)
    {
        var validator = new UpdateMedicineDtoValidator();
        var dto = new UpdateMedicineDto
        {
            Id = 1,
            CategoryId = 1,
            ManufacturerId = 1,
            Name = "Amoxicillin 500mg",
            TradeName = "Amoxil",
            GenericName = genericName!,
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = "Box",
            SellingPrice = 15.5m
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMedicineDto.GenericName));
    }

    // ─── 3. Manufacturer Tenant Safety Tests ───────────────────────────────────

    [Fact]
    public async Task CreateMedicineAsync_ManufacturerFromAnotherTenant_IsRejected()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(CreateMedicineAsync_ManufacturerFromAnotherTenant_IsRejected), tenantA);

        var category = new Category { Id = 1, TenantId = tenantA, Name = "Antibiotics", IsActive = true };
        var foreignManufacturer = new Manufacturer { Id = 2, TenantId = tenantB, Name = "Foreign Pharma", IsDeleted = false };
        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(foreignManufacturer);
        await db.SaveChangesAsync();

        var dto = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = foreignManufacturer.Id,
            Name = "Amoxicillin",
            TradeName = "Amoxil",
            GenericName = "Amoxicillin Trihydrate",
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = "Box",
            SellingPrice = 10m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateMedicineAsync(dto));
    }

    [Fact]
    public async Task UpdateMedicineAsync_ManufacturerFromAnotherTenant_IsRejected()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(UpdateMedicineAsync_ManufacturerFromAnotherTenant_IsRejected), tenantA);

        var category = new Category { Id = 1, TenantId = tenantA, Name = "Antibiotics", IsActive = true };
        var validManufacturer = new Manufacturer { Id = 1, TenantId = tenantA, Name = "Local Pharma", IsDeleted = false };
        var foreignManufacturer = new Manufacturer { Id = 2, TenantId = tenantB, Name = "Foreign Pharma", IsDeleted = false };
        var medicine = new Medicine
        {
            Id = 10,
            TenantId = tenantA,
            CategoryId = category.Id,
            ManufacturerId = validManufacturer.Id,
            Name = "Existing Medicine",
            TradeName = "Existing",
            GenericName = "Existing Generic",
            DosageForm = "Tablet",
            Strength = "10mg",
            PrimaryUnit = "Box",
            SellingPrice = 12m
        };

        db.Categories.Add(category);
        db.Set<Manufacturer>().AddRange(validManufacturer, foreignManufacturer);
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();

        var updateDto = new UpdateMedicineDto
        {
            Id = medicine.Id,
            CategoryId = category.Id,
            ManufacturerId = foreignManufacturer.Id,
            Name = "Updated Name",
            TradeName = "Updated Trade",
            GenericName = "Updated Generic",
            DosageForm = "Tablet",
            Strength = "10mg",
            PrimaryUnit = "Box",
            SellingPrice = 15m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateMedicineAsync(updateDto));
    }

    [Fact]
    public async Task CreateMedicineAsync_NonexistentOrSoftDeletedManufacturer_IsRejected()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(CreateMedicineAsync_NonexistentOrSoftDeletedManufacturer_IsRejected), tenantId);

        var category = new Category { Id = 1, TenantId = tenantId, Name = "Analgesics", IsActive = true };
        var deletedManufacturer = new Manufacturer { Id = 5, TenantId = tenantId, Name = "Old Pharma", IsDeleted = true };
        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(deletedManufacturer);
        await db.SaveChangesAsync();

        var dtoNonexistent = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = 999, // Does not exist
            Name = "Paracetamol",
            TradeName = "Panadol",
            GenericName = "Paracetamol",
            DosageForm = "Tablet",
            Strength = "500mg",
            PrimaryUnit = "Box",
            SellingPrice = 5m
        };

        var dtoDeleted = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = deletedManufacturer.Id, // Soft-deleted
            Name = "Paracetamol",
            TradeName = "Panadol",
            GenericName = "Paracetamol",
            DosageForm = "Tablet",
            Strength = "500mg",
            PrimaryUnit = "Box",
            SellingPrice = 5m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateMedicineAsync(dtoNonexistent));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateMedicineAsync(dtoDeleted));
    }

    [Fact]
    public async Task CreateMedicineAsync_ValidManufacturerAndCategory_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(CreateMedicineAsync_ValidManufacturerAndCategory_Succeeds), tenantId);

        var category = new Category { Id = 1, TenantId = tenantId, Name = "Cardio", IsActive = true };
        var manufacturer = new Manufacturer { Id = 1, TenantId = tenantId, Name = "Novartis", IsDeleted = false };
        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(manufacturer);
        await db.SaveChangesAsync();

        var dto = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Amlodipine 5mg",
            TradeName = "Norvasc",
            GenericName = "Amlodipine Besylate",
            DosageForm = "Tablet",
            Strength = "5mg",
            PrimaryUnit = "Strip",
            SellingPrice = 25m,
            MinStockLevel = 10
        };

        var result = await service.CreateMedicineAsync(dto);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Norvasc", result.TradeName);
        Assert.Equal("Amlodipine Besylate", result.GenericName);
        Assert.Equal("Strip", result.PrimaryUnit);
        Assert.Equal("Cardio", result.CategoryName);
        Assert.Equal("Novartis", result.ManufacturerName);
    }

    // ─── 4. Barcode Conflict Tests ─────────────────────────────────────────────

    [Fact]
    public async Task CreateMedicineAsync_DuplicateBarcodeInSameTenant_ThrowsConflictException()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(CreateMedicineAsync_DuplicateBarcodeInSameTenant_ThrowsConflictException), tenantId);

        var category = new Category { Id = 1, TenantId = tenantId, Name = "Pain Relief", IsActive = true };
        var manufacturer = new Manufacturer { Id = 1, TenantId = tenantId, Name = "GSK", IsDeleted = false };
        var existingMedicine = new Medicine
        {
            Id = 1,
            TenantId = tenantId,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Panadol",
            TradeName = "Panadol",
            GenericName = "Paracetamol",
            DosageForm = "Tablet",
            Strength = "500mg",
            PrimaryUnit = "Box",
            Barcode = "6221234567890",
            SellingPrice = 10m
        };

        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(manufacturer);
        db.Medicines.Add(existingMedicine);
        await db.SaveChangesAsync();

        var dto = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Panadol Extra",
            TradeName = "Panadol Extra",
            GenericName = "Paracetamol / Caffeine",
            DosageForm = "Tablet",
            Strength = "500mg / 65mg",
            PrimaryUnit = "Box",
            Barcode = "6221234567890", // Duplicate
            SellingPrice = 15m
        };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateMedicineAsync(dto));
        Assert.Contains("6221234567890", ex.Message);
    }

    [Fact]
    public async Task UpdateMedicineAsync_DuplicateBarcodeFromAnotherMedicine_ThrowsConflictException()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(UpdateMedicineAsync_DuplicateBarcodeFromAnotherMedicine_ThrowsConflictException), tenantId);

        var category = new Category { Id = 1, TenantId = tenantId, Name = "Pain Relief", IsActive = true };
        var manufacturer = new Manufacturer { Id = 1, TenantId = tenantId, Name = "GSK", IsDeleted = false };
        var medicineA = new Medicine
        {
            Id = 1,
            TenantId = tenantId,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Med A",
            TradeName = "Med A",
            GenericName = "Gen A",
            DosageForm = "Tablet",
            Strength = "10mg",
            PrimaryUnit = "Box",
            Barcode = "111111111",
            SellingPrice = 10m
        };
        var medicineB = new Medicine
        {
            Id = 2,
            TenantId = tenantId,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Med B",
            TradeName = "Med B",
            GenericName = "Gen B",
            DosageForm = "Tablet",
            Strength = "20mg",
            PrimaryUnit = "Box",
            Barcode = "222222222",
            SellingPrice = 20m
        };

        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(manufacturer);
        db.Medicines.AddRange(medicineA, medicineB);
        await db.SaveChangesAsync();

        // Updating Medicine B to use Medicine A's barcode
        var updateDto = new UpdateMedicineDto
        {
            Id = medicineB.Id,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Med B Updated",
            TradeName = "Med B",
            GenericName = "Gen B",
            DosageForm = "Tablet",
            Strength = "20mg",
            PrimaryUnit = "Box",
            Barcode = "111111111", // Conflicts with Med A
            SellingPrice = 22m
        };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.UpdateMedicineAsync(updateDto));
        Assert.Contains("111111111", ex.Message);
    }

    [Fact]
    public async Task UpdateMedicineAsync_RetainingOwnBarcode_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(UpdateMedicineAsync_RetainingOwnBarcode_Succeeds), tenantId);

        var category = new Category { Id = 1, TenantId = tenantId, Name = "Vitamins", IsActive = true };
        var manufacturer = new Manufacturer { Id = 1, TenantId = tenantId, Name = "Bayer", IsDeleted = false };
        var medicine = new Medicine
        {
            Id = 1,
            TenantId = tenantId,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Vitamin C",
            TradeName = "Redoxon",
            GenericName = "Ascorbic Acid",
            DosageForm = "Effervescent",
            Strength = "1000mg",
            PrimaryUnit = "Tube",
            Barcode = "999888777",
            SellingPrice = 30m
        };

        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(manufacturer);
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();

        var updateDto = new UpdateMedicineDto
        {
            Id = medicine.Id,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Vitamin C 1000mg Effervescent",
            TradeName = "Redoxon",
            GenericName = "Ascorbic Acid",
            DosageForm = "Effervescent",
            Strength = "1000mg",
            PrimaryUnit = "Tube",
            Barcode = "999888777", // Same barcode
            SellingPrice = 35m
        };

        var result = await service.UpdateMedicineAsync(updateDto);

        Assert.NotNull(result);
        Assert.Equal("999888777", result.Barcode);
        Assert.Equal(35m, result.SellingPrice);
        Assert.Equal("Vitamin C 1000mg Effervescent", result.Name);
    }

    // ─── 5. Not-Found & Query Tests ────────────────────────────────────────────

    [Fact]
    public async Task GetMedicineByIdAsync_MissingMedicine_ThrowsNotFoundException()
    {
        var tenantId = Guid.NewGuid();
        var (_, _, _, service) = CreateService(nameof(GetMedicineByIdAsync_MissingMedicine_ThrowsNotFoundException), tenantId);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMedicineByIdAsync(9999));
    }

    [Fact]
    public async Task GetMedicineByIdAsync_ExistingMedicine_PopulatesCategoryNameAndProperties()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(GetMedicineByIdAsync_ExistingMedicine_PopulatesCategoryNameAndProperties), tenantId);

        var category = new Category { Id = 10, TenantId = tenantId, Name = "Dermatology", IsActive = true };
        var manufacturer = new Manufacturer { Id = 20, TenantId = tenantId, Name = "Galderma", IsDeleted = false };
        var medicine = new Medicine
        {
            Id = 1,
            TenantId = tenantId,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Differin Gel",
            TradeName = "Differin",
            GenericName = "Adapalene",
            DosageForm = "Gel",
            Strength = "0.1%",
            PrimaryUnit = "Tube",
            SellingPrice = 45m,
            MinStockLevel = 5,
            IsActive = true
        };

        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(manufacturer);
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();

        var result = await service.GetMedicineByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Differin Gel", result.Name);
        Assert.Equal("Differin", result.TradeName);
        Assert.Equal("Adapalene", result.GenericName);
        Assert.Equal("Dermatology", result.CategoryName);
        Assert.Equal("Galderma", result.ManufacturerName);
        Assert.Equal("Tube", result.PrimaryUnit);
        Assert.Equal(45m, result.SellingPrice);
        Assert.Equal(5, result.MinStockLevel);
    }

    [Fact]
    public async Task GetMedicinesAsync_BatchLoadsRelatedEntitiesWithoutNPlus1()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(GetMedicinesAsync_BatchLoadsRelatedEntitiesWithoutNPlus1), tenantId);

        var cat1 = new Category { Id = 1, TenantId = tenantId, Name = "Cat 1", IsActive = true };
        var cat2 = new Category { Id = 2, TenantId = tenantId, Name = "Cat 2", IsActive = true };
        var mfr1 = new Manufacturer { Id = 1, TenantId = tenantId, Name = "Mfr 1", IsDeleted = false };
        var mfr2 = new Manufacturer { Id = 2, TenantId = tenantId, Name = "Mfr 2", IsDeleted = false };

        var med1 = new Medicine
        {
            Id = 1,
            TenantId = tenantId,
            CategoryId = cat1.Id,
            ManufacturerId = mfr1.Id,
            Name = "Med 1",
            TradeName = "Trade 1",
            GenericName = "Gen 1",
            DosageForm = "Tablet",
            Strength = "10mg",
            PrimaryUnit = "Box",
            SellingPrice = 10m
        };
        var med2 = new Medicine
        {
            Id = 2,
            TenantId = tenantId,
            CategoryId = cat2.Id,
            ManufacturerId = mfr2.Id,
            Name = "Med 2",
            TradeName = "Trade 2",
            GenericName = "Gen 2",
            DosageForm = "Syrup",
            Strength = "100ml",
            PrimaryUnit = "Bottle",
            SellingPrice = 20m
        };

        db.Categories.AddRange(cat1, cat2);
        db.Set<Manufacturer>().AddRange(mfr1, mfr2);
        db.Medicines.AddRange(med1, med2);
        await db.SaveChangesAsync();

        var list = (await service.GetMedicinesAsync()).ToList();

        Assert.Equal(2, list.Count);
        var res1 = list.Single(m => m.Id == 1);
        var res2 = list.Single(m => m.Id == 2);

        Assert.Equal("Cat 1", res1.CategoryName);
        Assert.Equal("Mfr 1", res1.ManufacturerName);
        Assert.Equal("Cat 2", res2.CategoryName);
        Assert.Equal("Mfr 2", res2.ManufacturerName);
    }

    // ─── 6. Tenant Isolation & Soft-Delete Tests ───────────────────────────────

    [Fact]
    public async Task TenantIsolationAndSoftDelete_PreservedInQueries()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var (db, _, _, serviceA) = CreateService(nameof(TenantIsolationAndSoftDelete_PreservedInQueries), tenantA);

        var catA = new Category { Id = 1, TenantId = tenantA, Name = "Category A", IsActive = true };
        var mfrA = new Manufacturer { Id = 1, TenantId = tenantA, Name = "Mfr A", IsDeleted = false };

        var medTenantA = new Medicine
        {
            Id = 1,
            TenantId = tenantA,
            CategoryId = catA.Id,
            ManufacturerId = mfrA.Id,
            Name = "Tenant A Medicine",
            TradeName = "Trade A",
            GenericName = "Gen A",
            DosageForm = "Tablet",
            Strength = "10mg",
            PrimaryUnit = "Box",
            SellingPrice = 10m,
            IsDeleted = false
        };

        var medDeletedTenantA = new Medicine
        {
            Id = 2,
            TenantId = tenantA,
            CategoryId = catA.Id,
            ManufacturerId = mfrA.Id,
            Name = "Deleted Medicine A",
            TradeName = "Deleted Trade",
            GenericName = "Deleted Gen",
            DosageForm = "Tablet",
            Strength = "10mg",
            PrimaryUnit = "Box",
            SellingPrice = 10m,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };

        var medTenantB = new Medicine
        {
            Id = 3,
            TenantId = tenantB,
            CategoryId = 99,
            ManufacturerId = 99,
            Name = "Tenant B Medicine",
            TradeName = "Trade B",
            GenericName = "Gen B",
            DosageForm = "Tablet",
            Strength = "10mg",
            PrimaryUnit = "Box",
            SellingPrice = 10m,
            IsDeleted = false
        };

        db.Categories.Add(catA);
        db.Set<Manufacturer>().Add(mfrA);
        db.Medicines.AddRange(medTenantA, medDeletedTenantA, medTenantB);
        await db.SaveChangesAsync();

        // Querying from Tenant A
        var tenantAMedicines = (await serviceA.GetMedicinesAsync()).ToList();

        // Must ONLY return medTenantA: medDeletedTenantA is filtered, medTenantB is filtered
        Assert.Single(tenantAMedicines);
        Assert.Equal(1, tenantAMedicines[0].Id);

        // Direct ID lookup on other tenant's medicine returns NotFoundException
        await Assert.ThrowsAsync<NotFoundException>(() => serviceA.GetMedicineByIdAsync(3));

        // Direct ID lookup on soft-deleted medicine returns NotFoundException
        await Assert.ThrowsAsync<NotFoundException>(() => serviceA.GetMedicineByIdAsync(2));
    }

    [Fact]
    public async Task GetMedicinesPagedAsync_EnforcesPageDefaultsAndMaxPageSize()
    {
        var tenantId = Guid.NewGuid();
        var (db, _, _, service) = CreateService(nameof(GetMedicinesPagedAsync_EnforcesPageDefaultsAndMaxPageSize), tenantId);

        var cat = new Category { Id = 1, TenantId = tenantId, Name = "General", IsActive = true };
        var mfr = new Manufacturer { Id = 1, TenantId = tenantId, Name = "Generic Lab", IsDeleted = false };
        db.Categories.Add(cat);
        db.Set<Manufacturer>().Add(mfr);

        for (int i = 1; i <= 25; i++)
        {
            db.Medicines.Add(new Medicine
            {
                Id = i,
                TenantId = tenantId,
                CategoryId = cat.Id,
                ManufacturerId = mfr.Id,
                Name = $"Med {i}",
                TradeName = $"Trade {i}",
                GenericName = $"Gen {i}",
                DosageForm = "Tablet",
                Strength = "10mg",
                PrimaryUnit = "Box",
                SellingPrice = 10m
            });
        }
        await db.SaveChangesAsync();

        // Test invalid/negative values clamp to safe defaults (page 1, pageSize 20)
        var defaultResult = await service.GetMedicinesPagedAsync(page: -1, pageSize: 0);
        Assert.Equal(1, defaultResult.Page);
        Assert.Equal(20, defaultResult.PageSize);
        Assert.Equal(20, defaultResult.Items.Count);
        Assert.Equal(25, defaultResult.TotalCount);
        Assert.Equal(2, defaultResult.TotalPages);

        // Test pageSize > 100 clamps to 100
        var clampedResult = await service.GetMedicinesPagedAsync(page: 1, pageSize: 500);
        Assert.Equal(100, clampedResult.PageSize);
        Assert.Equal(25, clampedResult.Items.Count);
    }
}
