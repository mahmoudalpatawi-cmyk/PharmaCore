using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Infrastructure.Identity;

namespace PharmaCore.UnitTests;

public class CurrentUserServiceTests
{
    private (CurrentUserService sut, DefaultHttpContext httpContext) CreateSut(ClaimsPrincipal? principal = null)
    {
        var context = new DefaultHttpContext();
        if (principal != null)
        {
            context.User = principal;
        }

        var accessor = new HttpContextAccessor { HttpContext = context };
        var sut = new CurrentUserService(accessor);
        return (sut, context);
    }

    [Fact]
    public void Properties_WhenAuthenticated_ResolvesClaimsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "123"),
            new Claim(ClaimTypes.Email, "pharmacist@test.com"),
            new Claim("TenantId", tenantId.ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Pharmacist),
            new Claim("branch_id", "45")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var (sut, _) = CreateSut(principal);

        Assert.True(sut.IsAuthenticated);
        Assert.Equal(123, sut.UserId);
        Assert.Equal(tenantId, sut.TenantId);
        Assert.Equal(AppRoles.Pharmacist, sut.Role);
        Assert.Equal(45, sut.BranchId);
        Assert.True(sut.IsInRole(AppRoles.Pharmacist));
        Assert.False(sut.IsInRole(AppRoles.Owner));
    }

    [Fact]
    public void Properties_WhenUnauthenticated_ReturnsDefaults()
    {
        var (sut, _) = CreateSut(new ClaimsPrincipal());

        Assert.False(sut.IsAuthenticated);
        Assert.Null(sut.UserId);
        Assert.Null(sut.TenantId);
        Assert.Null(sut.Role);
        Assert.Null(sut.BranchId);
        Assert.False(sut.IsInRole(AppRoles.Owner));
    }

    [Fact]
    public void CanAccessBranch_WhenUserIsOwnerWithoutBranch_ReturnsTrueForAnyBranch()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim("TenantId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Owner)
            // No branch_id claim
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var (sut, _) = CreateSut(principal);

        Assert.True(sut.CanAccessBranch(10));
        Assert.True(sut.CanAccessBranch(99));
    }

    [Fact]
    public void CanAccessBranch_WhenUserIsAdminWithoutBranch_ReturnsTrueForAnyBranch()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim("TenantId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Admin)
            // No branch_id claim
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var (sut, _) = CreateSut(principal);

        Assert.True(sut.CanAccessBranch(10));
        Assert.True(sut.CanAccessBranch(99));
    }

    [Fact]
    public void CanAccessBranch_WhenUserIsBranchBoundAdmin_AllowsOnlyAssignedBranch()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim("TenantId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Admin),
            new Claim("branch_id", "5")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var (sut, _) = CreateSut(principal);

        Assert.True(sut.CanAccessBranch(5));
        Assert.False(sut.CanAccessBranch(10));
    }

    [Fact]
    public void CanAccessBranch_WhenUserIsPharmacist_AllowsOnlyAssignedBranch()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "4"),
            new Claim("TenantId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Pharmacist),
            new Claim("branch_id", "7")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var (sut, _) = CreateSut(principal);

        Assert.True(sut.CanAccessBranch(7));
        Assert.False(sut.CanAccessBranch(8));
        Assert.False(sut.CanAccessBranch(1));
    }

    [Fact]
    public void CanAccessBranch_WhenUserIsCashier_AllowsOnlyAssignedBranch()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim("TenantId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Cashier),
            new Claim("branch_id", "2")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var (sut, _) = CreateSut(principal);

        Assert.True(sut.CanAccessBranch(2));
        Assert.False(sut.CanAccessBranch(1));
    }

    [Fact]
    public void CanAccessBranch_WhenUserIsPharmacistWithoutBranch_ReturnsFalse()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "6"),
            new Claim("TenantId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Pharmacist)
            // No branch_id
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var (sut, _) = CreateSut(principal);

        Assert.False(sut.CanAccessBranch(1));
    }
}
