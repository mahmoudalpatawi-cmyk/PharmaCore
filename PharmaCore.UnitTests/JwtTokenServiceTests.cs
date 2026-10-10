using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PharmaCore.Application.Identity;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Infrastructure.Identity;

namespace PharmaCore.UnitTests;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _settings = new()
    {
        SecretKey = "ThisIsASecretKeyForTestingPurposesOnlyWithAtLeast256BitsOfEntropy!",
        Issuer = "PharmaCoreTestIssuer",
        Audience = "PharmaCoreTestAudience",
        ExpiryMinutes = 60
    };

    private JwtTokenService CreateSut()
    {
        return new JwtTokenService(Options.Create(_settings));
    }

    [Fact]
    public async Task GenerateTokenAsync_EmitsExactlyOneCanonicalTenantIdClaim_AndNoLowercaseTenantId()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = 42,
            TenantId = tenantId,
            UserName = "user@example.com",
            Email = "user@example.com",
            FullName = "Test User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = new Role { Id = 1, Name = AppRoles.Admin }
        };

        var tokenString = await sut.GenerateTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        var tenantIdClaims = jwt.Claims.Where(c => c.Type == "TenantId").ToList();
        var lowercaseTenantIdClaims = jwt.Claims.Where(c => c.Type == "tenant_id").ToList();

        Assert.Single(tenantIdClaims);
        Assert.Equal(tenantId.ToString(), tenantIdClaims[0].Value);
        Assert.Empty(lowercaseTenantIdClaims);
    }

    [Fact]
    public async Task GenerateTokenAsync_WhenBranchIdIsNull_OmitsBranchIdClaim()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = Guid.NewGuid(),
            UserName = "owner@example.com",
            Email = "owner@example.com",
            FullName = "Owner User",
            IsActive = true,
            IsDeleted = false,
            BranchId = null,
            RoleId = 1,
            Role = new Role { Id = 1, Name = AppRoles.Owner }
        };

        var tokenString = await sut.GenerateTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "branch_id");
    }

    [Fact]
    public async Task GenerateTokenAsync_WhenBranchIdIsNotNull_IncludesBranchIdClaim()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = Guid.NewGuid(),
            UserName = "pharmacist@example.com",
            Email = "pharmacist@example.com",
            FullName = "Pharmacist User",
            IsActive = true,
            IsDeleted = false,
            BranchId = 15,
            RoleId = 3,
            Role = new Role { Id = 3, Name = AppRoles.Pharmacist }
        };

        var tokenString = await sut.GenerateTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        var branchClaim = jwt.Claims.FirstOrDefault(c => c.Type == "branch_id");
        Assert.NotNull(branchClaim);
        Assert.Equal("15", branchClaim.Value);
    }

    [Fact]
    public async Task GenerateTokenAsync_IncludesRequiredStandardClaims()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 77,
            TenantId = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FullName = "Test Subject",
            IsActive = true,
            IsDeleted = false,
            RoleId = 2,
            Role = new Role { Id = 2, Name = AppRoles.Admin }
        };

        var tokenString = await sut.GenerateTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "77");
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Name && c.Value == "Test Subject");
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrWhiteSpace(c.Value));
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == AppRoles.Admin);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateToken_WithValidParameters_PassesValidation()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 10,
            TenantId = Guid.NewGuid(),
            UserName = "user@pharma.com",
            Email = "user@pharma.com",
            FullName = "Valid User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = new Role { Id = 1, Name = AppRoles.Owner }
        };

        var tokenString = await sut.GenerateTokenAsync(user);

        var tokenParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();
        var principal = handler.ValidateToken(tokenString, tokenParams, out var validatedToken);

        Assert.NotNull(principal);
        Assert.NotNull(validatedToken);
        Assert.True(principal.IsInRole(AppRoles.Owner));
    }

    [Fact]
    public async Task ValidateToken_OwnerRoleClaim_SatisfiesOwnerPolicy_AndFailsAdminPolicy()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = Guid.NewGuid(),
            UserName = "owner@pharma.com",
            Email = "owner@pharma.com",
            FullName = "Owner User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = new Role { Id = 1, Name = AppRoles.Owner }
        };

        var tokenString = await sut.GenerateTokenAsync(user);

        var tokenParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear(); // Mirrors options.MapInboundClaims = false
        var principal = handler.ValidateToken(tokenString, tokenParams, out _);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.OwnerOnly, policy => policy.RequireRole(AppRoles.Owner));
            options.AddPolicy(AppPolicies.AdminOnly, policy => policy.RequireRole(AppRoles.Admin));
            options.AddPolicy(AppPolicies.RequireOwnerOrAdmin, policy => policy.RequireRole(AppRoles.Owner, AppRoles.Admin));
        });
        var authService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();

        var ownerResult = await authService.AuthorizeAsync(principal, null, AppPolicies.OwnerOnly);
        var adminResult = await authService.AuthorizeAsync(principal, null, AppPolicies.AdminOnly);
        var compositeResult = await authService.AuthorizeAsync(principal, null, AppPolicies.RequireOwnerOrAdmin);

        Assert.True(ownerResult.Succeeded);
        Assert.False(adminResult.Succeeded);
        Assert.True(compositeResult.Succeeded);
    }

    [Fact]
    public async Task ValidateToken_AdminRoleClaim_SatisfiesAdminPolicy_AndFailsOwnerPolicy()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 2,
            TenantId = Guid.NewGuid(),
            UserName = "admin@pharma.com",
            Email = "admin@pharma.com",
            FullName = "Admin User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 2,
            Role = new Role { Id = 2, Name = AppRoles.Admin }
        };

        var tokenString = await sut.GenerateTokenAsync(user);

        var tokenParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear(); // Mirrors options.MapInboundClaims = false
        var principal = handler.ValidateToken(tokenString, tokenParams, out _);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.OwnerOnly, policy => policy.RequireRole(AppRoles.Owner));
            options.AddPolicy(AppPolicies.AdminOnly, policy => policy.RequireRole(AppRoles.Admin));
        });
        var authService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();

        var ownerResult = await authService.AuthorizeAsync(principal, null, AppPolicies.OwnerOnly);
        var adminResult = await authService.AuthorizeAsync(principal, null, AppPolicies.AdminOnly);

        Assert.False(ownerResult.Succeeded);
        Assert.True(adminResult.Succeeded);
    }

    [Fact]
    public async Task ValidateToken_WithWrongIssuer_FailsValidation()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 10,
            TenantId = Guid.NewGuid(),
            UserName = "user@pharma.com",
            Email = "user@pharma.com",
            FullName = "User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = new Role { Id = 1, Name = AppRoles.Owner }
        };

        var tokenString = await sut.GenerateTokenAsync(user);

        var tokenParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = "DifferentIssuer",
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true
        };

        var handler = new JwtSecurityTokenHandler();
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            handler.ValidateToken(tokenString, tokenParams, out _));
    }

    [Fact]
    public async Task ValidateToken_WithWrongAudience_FailsValidation()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 10,
            TenantId = Guid.NewGuid(),
            UserName = "user@pharma.com",
            Email = "user@pharma.com",
            FullName = "User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = new Role { Id = 1, Name = AppRoles.Owner }
        };

        var tokenString = await sut.GenerateTokenAsync(user);

        var tokenParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = "WrongAudience",
            ValidateLifetime = true
        };

        var handler = new JwtSecurityTokenHandler();
        Assert.Throws<SecurityTokenInvalidAudienceException>(() =>
            handler.ValidateToken(tokenString, tokenParams, out _));
    }

    [Fact]
    public async Task ValidateToken_WithWrongSigningKey_FailsValidation()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = 10,
            TenantId = Guid.NewGuid(),
            UserName = "user@pharma.com",
            Email = "user@pharma.com",
            FullName = "User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = new Role { Id = 1, Name = AppRoles.Owner }
        };

        var tokenString = await sut.GenerateTokenAsync(user);

        var wrongKey = "AnotherCompletelyDifferentSigningKeyForTestingPurposeOnly1234567890!";
        var tokenParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(wrongKey)),
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true
        };

        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(tokenString, tokenParams, out _));
    }
}
