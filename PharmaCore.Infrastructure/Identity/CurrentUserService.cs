using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Domain.Entities.Identity;

namespace PharmaCore.Infrastructure.Identity;

/// <summary>
/// Infrastructure implementation of ICurrentUserService resolving ambient caller identity,
/// tenant boundaries, and branch scoping from the active HttpContext ClaimsPrincipal.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public int? UserId
    {
        get
        {
            var subClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User?.FindFirst("sub")?.Value;

            return int.TryParse(subClaim, out var id) ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var tenantClaim = User?.FindFirst("TenantId")?.Value
                           ?? User?.FindFirst("tenant_id")?.Value;

            return Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty
                ? tenantId
                : null;
        }
    }

    public int? BranchId
    {
        get
        {
            var branchClaim = User?.FindFirst("branch_id")?.Value;
            return int.TryParse(branchClaim, out var branchId) ? branchId : null;
        }
    }

    public string? Role
    {
        get
        {
            return User?.FindFirst(ClaimTypes.Role)?.Value
                ?? User?.FindFirst("role")?.Value;
        }
    }

    public bool IsInRole(string role)
    {
        if (!IsAuthenticated || string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return User?.IsInRole(role) == true
            || string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanAccessBranch(int targetBranchId)
    {
        if (!IsAuthenticated || targetBranchId <= 0)
        {
            return false;
        }

        // Owner with no specific branch assignment operates tenant-wide
        if (IsInRole(AppRoles.Owner) && !BranchId.HasValue)
        {
            return true;
        }

        // Admin with no specific branch assignment operates tenant-wide
        if (IsInRole(AppRoles.Admin) && !BranchId.HasValue)
        {
            return true;
        }

        // Any branch-bound user (Branch-bound Admin, Pharmacist, Cashier) can ONLY operate on their assigned branch
        if (BranchId.HasValue)
        {
            return BranchId.Value == targetBranchId;
        }

        return false;
    }
}
