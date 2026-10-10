using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaCore.Application.Identity.DTOs;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Infrastructure.Data;

namespace PharmaCore.Infrastructure.Identity;

/// <summary>
/// Infrastructure implementation of IAuthService for multi-tenant user authentication.
/// Resolves tenants safely, looks up tenant-scoped users, verifies passwords using
/// standard PBKDF2 hashing, and coordinates JWT token generation.
/// </summary>
public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid tenant code, email, or password.";

    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _jwtSettings = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
    }

    /// <summary>
    /// Executes the multi-tenant login workflow:
    /// 1. Resolves and validates the tenant by unique TenantCode.
    /// 2. Performs a tenant-scoped user query with .IgnoreQueryFilters() explicitly bound to the tenant ID.
    /// 3. Verifies the password hash using standard PBKDF2.
    /// 4. Generates and returns a signed JWT access token.
    /// Fails closed with a uniform message on all authentication failures to prevent enumeration.
    /// </summary>
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.TenantCode) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        // ─── Step 1: Tenant Resolution ───────────────────────────────────────
        // Query the Tenants root table (which only has the !IsDeleted query filter).
        var normalizedTenantCode = request.TenantCode.Trim().ToUpperInvariant();

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Code == normalizedTenantCode, cancellationToken);

        // Fail closed if tenant does not exist, is soft-deleted, or subscription has expired.
        if (tenant == null || tenant.IsDeleted || tenant.SubscriptionEndDate < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        // ─── Step 2: Tenant-Scoped User Lookup ────────────────────────────────
        // NOTE: ApplicationUser implements IMustHaveTenant. Because the caller is
        // not yet authenticated, CurrentTenantId would evaluate to Guid.Empty.
        // Therefore, .IgnoreQueryFilters() is strictly authorized ONLY at this boundary,
        // and MUST immediately re-apply explicit predicates for tenant isolation,
        // soft deletion, and user active status.
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _context.ApplicationUsers
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.TenantId == tenant.Id &&
                (u.NormalizedEmail == normalizedEmail || (u.Email != null && u.Email.ToUpper() == normalizedEmail)) &&
                !u.IsDeleted &&
                u.IsActive,
                cancellationToken);

        if (user == null || user.Role == null || string.IsNullOrWhiteSpace(user.Role.Name))
        {
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        // ─── Step 3: Password Verification ───────────────────────────────────
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        // Rehash password if standard algorithm upgraded (e.g., higher work factor)
        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // ─── Step 4: Token Issuance ───────────────────────────────────────────
        var accessToken = await _jwtTokenService.GenerateTokenAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = user.Role.Name,
            TenantId = user.TenantId,
            BranchId = user.BranchId
        };
    }
}
