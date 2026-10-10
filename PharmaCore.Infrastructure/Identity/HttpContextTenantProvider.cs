using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PharmaCore.Domain.Common;

namespace PharmaCore.Infrastructure.Identity;

/// <summary>
/// Resolves the current tenant identity from the authenticated HttpContext user claims.
/// Fails closed if the context is missing, the user is unauthenticated, or the tenant claim is missing/invalid.
/// </summary>
public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Standard claim type name used across PharmaCore for tenant identification.
    /// Also supports "tenant_id" for compatibility with standard JWT claim formatting.
    /// </summary>
    public const string TenantIdClaimType = "TenantId";
    public const string AlternativeTenantIdClaimType = "tenant_id";

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Returns the validated TenantId for the current authenticated request.
    /// Fails closed by throwing an exception if the context, authentication, or claim is invalid.
    /// Never returns Guid.Empty or an unvalidated fallback value.
    /// </summary>
    public Guid GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("Tenant context cannot be resolved outside an active HTTP request.");
        }

        var user = httpContext.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Cannot resolve tenant identity for an unauthenticated request.");
        }

        var tenantClaim = user.FindFirst(TenantIdClaimType) ?? user.FindFirst(AlternativeTenantIdClaimType);
        if (tenantClaim == null || string.IsNullOrWhiteSpace(tenantClaim.Value))
        {
            throw new UnauthorizedAccessException($"Tenant claim ('{TenantIdClaimType}' or '{AlternativeTenantIdClaimType}') is missing from the authenticated user.");
        }

        if (!Guid.TryParse(tenantClaim.Value, out var tenantId) || tenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException($"Tenant claim value '{tenantClaim.Value}' is not a valid non-empty GUID.");
        }

        return tenantId;
    }
}
