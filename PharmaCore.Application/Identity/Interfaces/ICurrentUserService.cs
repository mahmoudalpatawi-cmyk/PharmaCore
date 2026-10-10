using System;

namespace PharmaCore.Application.Identity.Interfaces;

/// <summary>
/// Abstraction for accessing ambient authenticated user identity, role, and branch authorization scope.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The authenticated user's integer identifier, or null if unauthenticated.
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// The authenticated user's tenant identifier, or null if unauthenticated.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// The authenticated user's assigned branch identifier, or null if tenant-wide/unassigned.
    /// </summary>
    int? BranchId { get; }

    /// <summary>
    /// The authenticated user's role name, or null if unauthenticated.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Indicates whether the current request is executed by an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Determines whether the current user belongs to the specified role.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Determines whether the current user has permission to operate on the specified target branch.
    /// Rules:
    /// - Owner or Admin with no branch assignment (BranchId == null): Can access any branch within their tenant.
    /// - Admin assigned to a branch: Can only access their assigned branch.
    /// - Pharmacist or Cashier: Can strictly access their assigned branch only.
    /// </summary>
    /// <param name="targetBranchId">The branch identifier being accessed or mutated.</param>
    /// <returns>True if access is authorized; otherwise false.</returns>
    bool CanAccessBranch(int targetBranchId);
}
