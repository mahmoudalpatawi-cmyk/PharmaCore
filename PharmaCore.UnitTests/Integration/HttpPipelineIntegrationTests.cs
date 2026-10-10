using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PharmaCore.Application.Identity.DTOs;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Infrastructure.Identity;
using Xunit;

namespace PharmaCore.UnitTests.Integration;

public class HttpPipelineIntegrationTests
{
    private ApplicationUser CreateUserWithRole(string roleName, int userId = 1, int? branchId = null)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = Guid.NewGuid(),
            UserName = $"user{userId}@pharmacy.com",
            Email = $"user{userId}@pharmacy.com",
            FullName = $"User {userId}",
            IsActive = true,
            IsDeleted = false,
            BranchId = branchId,
            RoleId = 1,
            Role = new Role { Id = 1, Name = roleName }
        };
    }

    private string GenerateExpiredToken(JwtSettings settings, ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new("TenantId", user.TenantId.ToString()),
            new("role", user.Role?.Name ?? "Unknown"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-30),
            expires: DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private async Task<string> GenerateUserTokenAsync(CustomWebApplicationFactory factory, ApplicationUser user)
    {
        using var scope = factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return await jwtService.GenerateTokenAsync(user);
    }

    private JwtSettings GetJwtSettings(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
    }

    // ─── A. Authentication Pipeline Tests ──────────────────────────────────────

    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedEndpoint_Returns401Unauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/test-auth/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidJwt_FromTokenService_PassesAuthentication_Returns200OK()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var user = CreateUserWithRole(AppRoles.Pharmacist);
        var token = await GenerateUserTokenAsync(factory, user);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/test-auth/protected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("authenticated_access_granted", content);
    }

    [Fact]
    public async Task InvalidOrTamperedJwt_Returns401Unauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var user = CreateUserWithRole(AppRoles.Pharmacist);
        var validToken = await GenerateUserTokenAsync(factory, user);

        // Tamper with the token's cryptographic signature
        var tamperedToken = validToken.Substring(0, validToken.Length - 6) + "TAMPER";

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);
        var response = await client.GetAsync("/api/test-auth/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredJwt_Returns401Unauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var settings = GetJwtSettings(factory);
        var user = CreateUserWithRole(AppRoles.Pharmacist);
        var expiredToken = GenerateExpiredToken(settings, user);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        var response = await client.GetAsync("/api/test-auth/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─── B. Authorization Policy Pipeline Tests ────────────────────────────────

    [Fact]
    public async Task AuthenticatedUser_WithoutRequiredRole_Returns403Forbidden()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Cashier attempts to access Owner-only endpoint
        var user = CreateUserWithRole(AppRoles.Cashier);
        var token = await GenerateUserTokenAsync(factory, user);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/test-auth/owner-only");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OwnerToken_SatisfiesOwnerPolicy_Returns200OK()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var user = CreateUserWithRole(AppRoles.Owner);
        var token = await GenerateUserTokenAsync(factory, user);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/test-auth/owner-only");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("owner_access_granted", content);
    }

    [Fact]
    public async Task AdminToken_SatisfiesAdminPolicy_Returns200OK()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var user = CreateUserWithRole(AppRoles.Admin);
        var token = await GenerateUserTokenAsync(factory, user);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/test-auth/admin-only");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("admin_access_granted", content);
    }

    [Fact]
    public async Task AdminToken_FailsOwnerPolicy_Returns403Forbidden()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var user = CreateUserWithRole(AppRoles.Admin);
        var token = await GenerateUserTokenAsync(factory, user);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/test-auth/owner-only");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── C. Login Rate Limiting Pipeline Tests ─────────────────────────────────

    [Fact]
    public async Task RepeatedLoginRequests_ExceedingLimit_Returns429TooManyRequests_WithRetryAfter()
    {
        // Configure deterministic test rate limiter: 3 permits per 60 seconds
        var overrides = new Dictionary<string, string?>
        {
            { "RateLimiting:Login:PermitLimit", "3" },
            { "RateLimiting:Login:WindowSeconds", "60" }
        };

        using var factory = new CustomWebApplicationFactory(configurationOverrides: overrides);
        var client = factory.CreateClient();

        var requestBody = new LoginRequestDto
        {
            TenantCode = "TEST-TENANT",
            Email = "rate_limit_test@example.com",
            Password = "Password123!"
        };

        // Requests 1, 2, 3: Within permit limit (fail authentication with 401, but NOT 429)
        for (int i = 1; i <= 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", requestBody);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // Request 4: Exceeds permit limit of 3 -> MUST return 429 Too Many Requests
        var throttledResponse = await client.PostAsJsonAsync("/api/auth/login", requestBody);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttledResponse.StatusCode);
        Assert.True(throttledResponse.Headers.Contains("Retry-After"));

        var problemDetails = await throttledResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(429, problemDetails.Status);
        Assert.Equal("Too Many Requests", problemDetails.Title);
        Assert.Equal("Too many login attempts. Please try again later.", problemDetails.Detail);
    }

    [Fact]
    public async Task LoginRequests_BelowThreshold_AreNotThrottledByRateLimiter()
    {
        var overrides = new Dictionary<string, string?>
        {
            { "RateLimiting:Login:PermitLimit", "5" },
            { "RateLimiting:Login:WindowSeconds", "60" }
        };

        using var factory = new CustomWebApplicationFactory(configurationOverrides: overrides);
        var client = factory.CreateClient();

        var requestBody = new LoginRequestDto
        {
            TenantCode = "TEST-TENANT",
            Email = "valid_rate_test@example.com",
            Password = "Password123!"
        };

        // Send 2 requests under a limit of 5
        var response1 = await client.PostAsJsonAsync("/api/auth/login", requestBody);
        var response2 = await client.PostAsJsonAsync("/api/auth/login", requestBody);

        Assert.NotEqual(HttpStatusCode.TooManyRequests, response1.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response2.StatusCode);
    }
}
