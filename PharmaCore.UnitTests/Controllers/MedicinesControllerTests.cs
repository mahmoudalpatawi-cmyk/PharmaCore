using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PharmaCore.Application.Catalog.DTOs;
using PharmaCore.Application.Common.DTOs;
using PharmaCore.Application.Identity;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Infrastructure.Data;
using PharmaCore.UnitTests.Integration;
using Xunit;

namespace PharmaCore.UnitTests.Controllers;

public class MedicinesControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private ApplicationUser CreateUserWithRole(string roleName, Guid tenantId, int userId = 1)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            UserName = $"user_{roleName.ToLower()}@pharmacy.com",
            Email = $"user_{roleName.ToLower()}@pharmacy.com",
            FullName = $"{roleName} User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = new Role { Id = 1, Name = roleName }
        };
    }

    private async Task<string> GenerateUserTokenAsync(CustomWebApplicationFactory factory, ApplicationUser user)
    {
        using var scope = factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return await jwtService.GenerateTokenAsync(user);
    }

    private async Task<(Category Category, Manufacturer Manufacturer)> SeedBaseCatalogAsync(
        CustomWebApplicationFactory factory,
        Guid tenantId,
        int categoryId = 1,
        int manufacturerId = 1)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var category = new Category
        {
            Id = categoryId,
            TenantId = tenantId,
            Name = "Antibiotics",
            IsActive = true
        };

        var manufacturer = new Manufacturer
        {
            Id = manufacturerId,
            TenantId = tenantId,
            Name = "Pfizer",
            IsDeleted = false
        };

        db.Categories.Add(category);
        db.Set<Manufacturer>().Add(manufacturer);
        await db.SaveChangesAsync();

        return (category, manufacturer);
    }

    // ─── 1. Authentication & Authorization ─────────────────────────────────────

    [Fact]
    public async Task UnauthenticatedAccess_ToGetAll_Returns401Unauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/medicines");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cashier_ReadingMedicines_FailsRequirePharmacyStaff_Returns403Forbidden()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var cashier = CreateUserWithRole(AppRoles.Cashier, tenantId);
        var token = await GenerateUserTokenAsync(factory, cashier);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/medicines");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pharmacist_ReadingMedicines_SatisfiesRequirePharmacyStaff_Returns200OK()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var pharmacist = CreateUserWithRole(AppRoles.Pharmacist, tenantId);
        var token = await GenerateUserTokenAsync(factory, pharmacist);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/medicines");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<MedicineDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Pharmacist_CreatingMedicine_FailsRequireOwnerOrAdmin_Returns403Forbidden()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var pharmacist = CreateUserWithRole(AppRoles.Pharmacist, tenantId);
        var token = await GenerateUserTokenAsync(factory, pharmacist);

        var dto = new CreateMedicineDto
        {
            CategoryId = 1,
            ManufacturerId = 1,
            Name = "Amoxicillin",
            TradeName = "Amoxil",
            GenericName = "Amoxicillin",
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = "Box",
            SellingPrice = 10m
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync("/api/medicines", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── 2. Validation & Service Invocation Guard ──────────────────────────────

    [Fact]
    public async Task Create_InvalidDto_MissingPrimaryUnitAndName_Returns400ValidationProblem()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var admin = CreateUserWithRole(AppRoles.Admin, tenantId);
        var token = await GenerateUserTokenAsync(factory, admin);

        var invalidDto = new CreateMedicineDto
        {
            CategoryId = 1,
            ManufacturerId = 1,
            Name = "", // Invalid
            TradeName = "Amoxil",
            GenericName = "Amoxicillin",
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = "", // Invalid
            SellingPrice = -5m // Invalid
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync("/api/medicines", invalidDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateMedicineDto.Name)));
        Assert.True(problem.Errors.ContainsKey(nameof(CreateMedicineDto.PrimaryUnit)));
        Assert.True(problem.Errors.ContainsKey(nameof(CreateMedicineDto.SellingPrice)));
    }

    [Fact]
    public async Task Update_RouteIdMismatch_Returns400BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var admin = CreateUserWithRole(AppRoles.Admin, tenantId);
        var token = await GenerateUserTokenAsync(factory, admin);

        var dto = new UpdateMedicineDto
        {
            Id = 5,
            CategoryId = 1,
            ManufacturerId = 1,
            Name = "Amoxicillin",
            TradeName = "Amoxil",
            GenericName = "Amoxicillin",
            DosageForm = "Capsule",
            Strength = "500mg",
            PrimaryUnit = "Box",
            SellingPrice = 10m
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PutAsJsonAsync("/api/medicines/10", dto); // Route 10 != DTO 5

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Contains("Route ID '10' does not match payload ID '5'", problem.Detail);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns400BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var pharmacist = CreateUserWithRole(AppRoles.Pharmacist, tenantId);
        var token = await GenerateUserTokenAsync(factory, pharmacist);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/medicines/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Contains("Medicine ID must be greater than zero", problem.Detail);
    }

    // ─── 3. Successful CRUD & Location Header ───────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201Created_WithUsableLocation()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var admin = CreateUserWithRole(AppRoles.Admin, tenantId);
        var token = await GenerateUserTokenAsync(factory, admin);
        var (category, manufacturer) = await SeedBaseCatalogAsync(factory, tenantId);

        var validDto = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Augmentin 1g",
            TradeName = "Augmentin",
            GenericName = "Amoxicillin / Clavulanate",
            DosageForm = "Tablet",
            Strength = "1g",
            PrimaryUnit = "Box",
            Barcode = "6221112223334",
            SellingPrice = 55m,
            MinStockLevel = 10
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync("/api/medicines", validDto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<MedicineDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Augmentin", created.TradeName);
        Assert.Equal("Antibiotics", created.CategoryName);
        Assert.Equal("Pfizer", created.ManufacturerName);

        // Verify Location header can be retrieved
        var getResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<MedicineDto>(JsonOptions);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404NotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var pharmacist = CreateUserWithRole(AppRoles.Pharmacist, tenantId);
        var token = await GenerateUserTokenAsync(factory, pharmacist);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/medicines/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task Create_DuplicateBarcode_Returns409Conflict()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var admin = CreateUserWithRole(AppRoles.Admin, tenantId);
        var token = await GenerateUserTokenAsync(factory, admin);
        var (category, manufacturer) = await SeedBaseCatalogAsync(factory, tenantId);

        var dto1 = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Panadol Advance",
            TradeName = "Panadol",
            GenericName = "Paracetamol",
            DosageForm = "Tablet",
            Strength = "500mg",
            PrimaryUnit = "Box",
            Barcode = "5000000000001",
            SellingPrice = 12m
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res1 = await client.PostAsJsonAsync("/api/medicines", dto1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        // Attempting to create second medicine with same barcode
        var dto2 = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Panadol Extra",
            TradeName = "Panadol Extra",
            GenericName = "Paracetamol / Caffeine",
            DosageForm = "Tablet",
            Strength = "500mg / 65mg",
            PrimaryUnit = "Box",
            Barcode = "5000000000001", // Duplicate
            SellingPrice = 18m
        };

        var res2 = await client.PostAsJsonAsync("/api/medicines", dto2);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
        var problem = await res2.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflict", problem.Title);
    }

    [Fact]
    public async Task Update_RetainingOwnBarcode_Returns200OK()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var admin = CreateUserWithRole(AppRoles.Admin, tenantId);
        var token = await GenerateUserTokenAsync(factory, admin);
        var (category, manufacturer) = await SeedBaseCatalogAsync(factory, tenantId);

        var createDto = new CreateMedicineDto
        {
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Cataflam 50mg",
            TradeName = "Cataflam",
            GenericName = "Diclofenac Potassium",
            DosageForm = "Tablet",
            Strength = "50mg",
            PrimaryUnit = "Strip",
            Barcode = "777888999",
            SellingPrice = 20m
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await client.PostAsJsonAsync("/api/medicines", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MedicineDto>(JsonOptions);
        Assert.NotNull(created);

        var updateDto = new UpdateMedicineDto
        {
            Id = created.Id,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Cataflam 50mg Updated",
            TradeName = "Cataflam",
            GenericName = "Diclofenac Potassium",
            DosageForm = "Tablet",
            Strength = "50mg",
            PrimaryUnit = "Strip",
            Barcode = "777888999", // Retaining own barcode
            SellingPrice = 25m
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/medicines/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MedicineDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(25m, updated.SellingPrice);
        Assert.Equal("Cataflam 50mg Updated", updated.Name);
    }

    // ─── 4. Pagination, Search & Tenant Isolation ──────────────────────────────

    [Fact]
    public async Task GetAll_PaginationDefaultsAndSearch_WorksAsExpected()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantId = Guid.NewGuid();
        var pharmacist = CreateUserWithRole(AppRoles.Pharmacist, tenantId);
        var token = await GenerateUserTokenAsync(factory, pharmacist);
        var (category, manufacturer) = await SeedBaseCatalogAsync(factory, tenantId);

        // Seed 5 medicines
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (int i = 1; i <= 5; i++)
            {
                db.Medicines.Add(new Medicine
                {
                    Id = i,
                    TenantId = tenantId,
                    CategoryId = category.Id,
                    ManufacturerId = manufacturer.Id,
                    Name = $"Medicine {i}",
                    TradeName = i % 2 == 0 ? "Aspirin" : "Paracetamol",
                    GenericName = $"Generic {i}",
                    DosageForm = "Tablet",
                    Strength = $"{i * 10}mg",
                    PrimaryUnit = "Box",
                    SellingPrice = 10m * i
                });
            }
            await db.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Page 1 with pageSize 2 -> should return 2 items, totalCount 5, totalPages 3
        var pagedResponse = await client.GetAsync("/api/medicines?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, pagedResponse.StatusCode);
        var pageResult = await pagedResponse.Content.ReadFromJsonAsync<PagedResultDto<MedicineDto>>(JsonOptions);
        Assert.NotNull(pageResult);
        Assert.Equal(2, pageResult.Items.Count);
        Assert.Equal(5, pageResult.TotalCount);
        Assert.Equal(1, pageResult.Page);
        Assert.Equal(2, pageResult.PageSize);
        Assert.Equal(3, pageResult.TotalPages);
        Assert.True(pageResult.HasNextPage);
        Assert.False(pageResult.HasPreviousPage);

        // Search for "Aspirin" -> items 2 and 4
        var searchResponse = await client.GetAsync("/api/medicines?search=Aspirin");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<PagedResultDto<MedicineDto>>(JsonOptions);
        Assert.NotNull(searchResult);
        Assert.Equal(2, searchResult.TotalCount);
        Assert.All(searchResult.Items, item => Assert.Equal("Aspirin", item.TradeName));
    }

    [Fact]
    public async Task GetAll_TenantIsolation_DoesNotExposeAnotherTenantsMedicines()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var (catA, mfrA) = await SeedBaseCatalogAsync(factory, tenantA, categoryId: 10, manufacturerId: 10);
        var (catB, mfrB) = await SeedBaseCatalogAsync(factory, tenantB, categoryId: 20, manufacturerId: 20);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Medicines.Add(new Medicine
            {
                Id = 100,
                TenantId = tenantA,
                CategoryId = catA.Id,
                ManufacturerId = mfrA.Id,
                Name = "Tenant A Only Medicine",
                TradeName = "A-Med",
                GenericName = "A-Gen",
                DosageForm = "Tablet",
                Strength = "10mg",
                PrimaryUnit = "Box",
                SellingPrice = 10m
            });
            db.Medicines.Add(new Medicine
            {
                Id = 200,
                TenantId = tenantB,
                CategoryId = catB.Id,
                ManufacturerId = mfrB.Id,
                Name = "Tenant B Secret Medicine",
                TradeName = "B-Med",
                GenericName = "B-Gen",
                DosageForm = "Tablet",
                Strength = "10mg",
                PrimaryUnit = "Box",
                SellingPrice = 20m
            });
            await db.SaveChangesAsync();
        }

        var pharmacistA = CreateUserWithRole(AppRoles.Pharmacist, tenantA);
        var tokenA = await GenerateUserTokenAsync(factory, pharmacistA);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var response = await client.GetAsync("/api/medicines");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<MedicineDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Tenant A Only Medicine", result.Items[0].Name);

        // Direct lookup of Tenant B medicine returns 404
        var directResponse = await client.GetAsync("/api/medicines/200");
        Assert.Equal(HttpStatusCode.NotFound, directResponse.StatusCode);
    }
}
