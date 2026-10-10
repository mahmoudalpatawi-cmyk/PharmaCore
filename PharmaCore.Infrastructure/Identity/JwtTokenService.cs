using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Domain.Entities.Identity;

namespace PharmaCore.Infrastructure.Identity;

/// <summary>
/// Infrastructure implementation of IJwtTokenService that generates HMAC-SHA256 signed
/// JSON Web Tokens (JWT) based on configured JwtSettings and validated ApplicationUser details.
/// Database-independent: receives an already validated user with Role navigation loaded.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));

        if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey) || Encoding.UTF8.GetByteCount(_jwtSettings.SecretKey) < 32)
        {
            throw new InvalidOperationException("JwtSettings:SecretKey is missing or shorter than 32 bytes.");
        }

        if (string.IsNullOrWhiteSpace(_jwtSettings.Issuer))
        {
            throw new InvalidOperationException("JwtSettings:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(_jwtSettings.Audience))
        {
            throw new InvalidOperationException("JwtSettings:Audience is required.");
        }

        if (_jwtSettings.ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException("JwtSettings:ExpiryMinutes must be greater than zero.");
        }
    }

    /// <summary>
    /// Generates an HMAC-SHA256 signed JWT containing the canonical security claims for the specified user.
    /// Fails closed if any required user security properties or role data are missing/invalid.
    /// </summary>
    /// <param name="user">The authenticated ApplicationUser with loaded Role.</param>
    /// <returns>Signed JWT token string.</returns>
    public Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        // ─── Fail-Closed Validation ──────────────────────────────────────────
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User cannot be null.");
        }

        if (user.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("User must have a valid non-empty TenantId to generate a token.");
        }

        if (user.Id <= 0)
        {
            throw new InvalidOperationException("User must have a valid positive Id to generate a token.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("User must have a valid Email to generate a token.");
        }

        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            throw new InvalidOperationException("User must have a valid FullName to generate a token.");
        }

        if (user.Role == null || string.IsNullOrWhiteSpace(user.Role.Name))
        {
            throw new InvalidOperationException("User Role must be loaded with a valid Name to generate a token.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Cannot generate a token for an inactive user.");
        }

        if (user.IsDeleted)
        {
            throw new InvalidOperationException("Cannot generate a token for a deleted user.");
        }

        // ─── Claims Construction ─────────────────────────────────────────────
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new("TenantId", user.TenantId.ToString()),
            new("role", user.Role.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // BranchId claim: Included ONLY when user.BranchId has a value; omitted otherwise.
        if (user.BranchId.HasValue)
        {
            claims.Add(new Claim("branch_id", user.BranchId.Value.ToString()));
        }

        // ─── Token Creation ──────────────────────────────────────────────────
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var utcNow = DateTime.UtcNow;
        var expires = utcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(tokenDescriptor);

        return Task.FromResult(tokenString);
    }
}
