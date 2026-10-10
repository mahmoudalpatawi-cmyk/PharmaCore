using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PharmaCore.Application.Catalog.Interfaces;
using PharmaCore.Application.Catalog.Services;
using PharmaCore.Application.Finance.Interfaces;
using PharmaCore.Application.Finance.Services;
using PharmaCore.Application.Inventory.Interfaces;
using PharmaCore.Application.Inventory.Services;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Application.Purchasing.Interfaces;
using PharmaCore.Application.Purchasing.Services;
using PharmaCore.Application.Sales.Interfaces;
using PharmaCore.Application.Sales.Services;
using PharmaCore.Application.Transfers.Interfaces;
using PharmaCore.Application.Transfers.Services;
using Microsoft.AspNetCore.Identity;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Infrastructure.Data;
using PharmaCore.Infrastructure.Identity;
using PharmaCore.Infrastructure.Repositories;

namespace PharmaCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MahmoudConnection");

        // ─── Database ──────────────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // ─── Repository & Unit of Work ─────────────────────────────────────────
        // Registered as Scoped so they share the same ApplicationDbContext instance
        // per request. This guarantees that all repository mutations and the final
        // SaveChangesAsync are committed together in a single EF Core transaction.
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ─── Tenant & Context Resolution ──────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

        // ─── Security & Cryptography ───────────────────────────────────────────
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ─── Validation ───────────────────────────────────────────────────────
        services.AddScoped<IValidator<PharmaCore.Application.Identity.DTOs.LoginRequestDto>, PharmaCore.Application.Identity.Validators.LoginRequestDtoValidator>();

        // ─── Object Mapping ───────────────────────────────────────────────────
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(PharmaCore.Application.Catalog.Mappings.CatalogMappingProfile).Assembly));

        // ─── Application Services ──────────────────────────────────────────────
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IPurchasingService, PurchasingService>();
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<ITransferDocumentService, TransferDocumentService>();

        return services;
    }
}
