using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using PharmaCore.Application.Identity;
using PharmaCore.Domain.Entities.Identity;

namespace PharmaCore.UnitTests;

public class AuthorizationPolicyTests
{
    private readonly IAuthorizationService _authService;

    public AuthorizationPolicyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.OwnerOnly, policy =>
                policy.RequireRole(AppRoles.Owner));

            options.AddPolicy(AppPolicies.AdminOnly, policy =>
                policy.RequireRole(AppRoles.Admin));

            options.AddPolicy(AppPolicies.PharmacistOnly, policy =>
                policy.RequireRole(AppRoles.Pharmacist));

            options.AddPolicy(AppPolicies.CashierOnly, policy =>
                policy.RequireRole(AppRoles.Cashier));

            options.AddPolicy(AppPolicies.RequireOwnerOrAdmin, policy =>
                policy.RequireRole(AppRoles.Owner, AppRoles.Admin));

            options.AddPolicy(AppPolicies.RequirePharmacyStaff, policy =>
                policy.RequireRole(AppRoles.Owner, AppRoles.Admin, AppRoles.Pharmacist));

            options.AddPolicy(AppPolicies.RequirePosAccess, policy =>
                policy.RequireRole(AppRoles.Owner, AppRoles.Admin, AppRoles.Cashier));
        });

        var provider = services.BuildServiceProvider();
        _authService = provider.GetRequiredService<IAuthorizationService>();
    }

    private ClaimsPrincipal CreatePrincipalWithRole(string? role, bool authenticated = true)
    {
        if (!authenticated || role == null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task OwnerOnly_AllowsOwner_RejectsOtherRoles()
    {
        var owner = CreatePrincipalWithRole(AppRoles.Owner);
        var admin = CreatePrincipalWithRole(AppRoles.Admin);
        var pharmacist = CreatePrincipalWithRole(AppRoles.Pharmacist);

        var ownerResult = await _authService.AuthorizeAsync(owner, null, AppPolicies.OwnerOnly);
        var adminResult = await _authService.AuthorizeAsync(admin, null, AppPolicies.OwnerOnly);
        var pharmacistResult = await _authService.AuthorizeAsync(pharmacist, null, AppPolicies.OwnerOnly);

        Assert.True(ownerResult.Succeeded);
        Assert.False(adminResult.Succeeded);
        Assert.False(pharmacistResult.Succeeded);
    }

    [Fact]
    public async Task AdminOnly_AllowsAdmin_RejectsOtherRoles()
    {
        var admin = CreatePrincipalWithRole(AppRoles.Admin);
        var owner = CreatePrincipalWithRole(AppRoles.Owner);

        var adminResult = await _authService.AuthorizeAsync(admin, null, AppPolicies.AdminOnly);
        var ownerResult = await _authService.AuthorizeAsync(owner, null, AppPolicies.AdminOnly);

        Assert.True(adminResult.Succeeded);
        Assert.False(ownerResult.Succeeded);
    }

    [Fact]
    public async Task PharmacistOnly_AllowsPharmacist_RejectsOtherRoles()
    {
        var pharmacist = CreatePrincipalWithRole(AppRoles.Pharmacist);
        var cashier = CreatePrincipalWithRole(AppRoles.Cashier);

        var pharmacistResult = await _authService.AuthorizeAsync(pharmacist, null, AppPolicies.PharmacistOnly);
        var cashierResult = await _authService.AuthorizeAsync(cashier, null, AppPolicies.PharmacistOnly);

        Assert.True(pharmacistResult.Succeeded);
        Assert.False(cashierResult.Succeeded);
    }

    [Fact]
    public async Task CashierOnly_AllowsCashier_RejectsOtherRoles()
    {
        var cashier = CreatePrincipalWithRole(AppRoles.Cashier);
        var pharmacist = CreatePrincipalWithRole(AppRoles.Pharmacist);

        var cashierResult = await _authService.AuthorizeAsync(cashier, null, AppPolicies.CashierOnly);
        var pharmacistResult = await _authService.AuthorizeAsync(pharmacist, null, AppPolicies.CashierOnly);

        Assert.True(cashierResult.Succeeded);
        Assert.False(pharmacistResult.Succeeded);
    }

    [Fact]
    public async Task RequireOwnerOrAdmin_AllowsBothOwnerAndAdmin_RejectsStaff()
    {
        var owner = CreatePrincipalWithRole(AppRoles.Owner);
        var admin = CreatePrincipalWithRole(AppRoles.Admin);
        var pharmacist = CreatePrincipalWithRole(AppRoles.Pharmacist);

        var ownerResult = await _authService.AuthorizeAsync(owner, null, AppPolicies.RequireOwnerOrAdmin);
        var adminResult = await _authService.AuthorizeAsync(admin, null, AppPolicies.RequireOwnerOrAdmin);
        var pharmacistResult = await _authService.AuthorizeAsync(pharmacist, null, AppPolicies.RequireOwnerOrAdmin);

        Assert.True(ownerResult.Succeeded);
        Assert.True(adminResult.Succeeded);
        Assert.False(pharmacistResult.Succeeded);
    }

    [Fact]
    public async Task RequirePharmacyStaff_AllowsOwnerAdminPharmacist_RejectsCashier()
    {
        var owner = CreatePrincipalWithRole(AppRoles.Owner);
        var admin = CreatePrincipalWithRole(AppRoles.Admin);
        var pharmacist = CreatePrincipalWithRole(AppRoles.Pharmacist);
        var cashier = CreatePrincipalWithRole(AppRoles.Cashier);

        Assert.True((await _authService.AuthorizeAsync(owner, null, AppPolicies.RequirePharmacyStaff)).Succeeded);
        Assert.True((await _authService.AuthorizeAsync(admin, null, AppPolicies.RequirePharmacyStaff)).Succeeded);
        Assert.True((await _authService.AuthorizeAsync(pharmacist, null, AppPolicies.RequirePharmacyStaff)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(cashier, null, AppPolicies.RequirePharmacyStaff)).Succeeded);
    }

    [Fact]
    public async Task RequirePosAccess_AllowsOwnerAdminCashier_RejectsPharmacist()
    {
        var owner = CreatePrincipalWithRole(AppRoles.Owner);
        var admin = CreatePrincipalWithRole(AppRoles.Admin);
        var pharmacist = CreatePrincipalWithRole(AppRoles.Pharmacist);
        var cashier = CreatePrincipalWithRole(AppRoles.Cashier);

        Assert.True((await _authService.AuthorizeAsync(owner, null, AppPolicies.RequirePosAccess)).Succeeded);
        Assert.True((await _authService.AuthorizeAsync(admin, null, AppPolicies.RequirePosAccess)).Succeeded);
        Assert.True((await _authService.AuthorizeAsync(cashier, null, AppPolicies.RequirePosAccess)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(pharmacist, null, AppPolicies.RequirePosAccess)).Succeeded);
    }

    [Fact]
    public async Task UnauthenticatedUser_FailsAllPolicies()
    {
        var anonymous = CreatePrincipalWithRole(null, authenticated: false);

        Assert.False((await _authService.AuthorizeAsync(anonymous, null, AppPolicies.OwnerOnly)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(anonymous, null, AppPolicies.AdminOnly)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(anonymous, null, AppPolicies.PharmacistOnly)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(anonymous, null, AppPolicies.CashierOnly)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(anonymous, null, AppPolicies.RequireOwnerOrAdmin)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(anonymous, null, AppPolicies.RequirePharmacyStaff)).Succeeded);
        Assert.False((await _authService.AuthorizeAsync(anonymous, null, AppPolicies.RequirePosAccess)).Succeeded);
    }
}
