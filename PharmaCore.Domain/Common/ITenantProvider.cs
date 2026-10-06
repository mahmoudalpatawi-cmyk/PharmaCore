namespace PharmaCore.Domain.Common;

/// <summary>
/// Abstraction for resolving the current tenant identity.
/// Implemented in the Infrastructure/Web layer (e.g., from JWT claims or request headers).
/// Injected into ApplicationDbContext to power global query filters.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Returns the TenantId for the current executing request/scope.
    /// Must never return Guid.Empty in a live request; throw if the tenant cannot be resolved.
    /// </summary>
    Guid GetTenantId();
}
