using System;
using Microsoft.AspNetCore.Http;
using PharmaCore.Domain.Common;

namespace PharmaCore.Infrastructure.Identity;

/// <summary>
/// Tenant provider implementation resolving the active tenant from the current HTTP context.
/// Currently configured with a fallback dummy Guid until full JWT claims integration is wired up.
/// </summary>
public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Default development/migration dummy tenant ID
    private static readonly Guid DummyTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid GetTenantId()
    {
        // Placeholder returning dummy Guid; will be linked to authenticated JWT claims in subsequent phase.
        return DummyTenantId;
    }
}
