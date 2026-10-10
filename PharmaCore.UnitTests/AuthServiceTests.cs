using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaCore.Application.Identity.DTOs;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Infrastructure.Data;
using PharmaCore.Infrastructure.Identity;

namespace PharmaCore.UnitTests;

public class AuthServiceTests
{
    private class TestJwtTokenService : IJwtTokenService
    {
        public string ExpectedToken { get; set; } = "mock.jwt.token";
        public ApplicationUser? LastPassedUser { get; private set; }

        public Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            LastPassedUser = user;
            return Task.FromResult(ExpectedToken);
        }
    }

    private ApplicationDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options, null);
    }

    private (AuthService authService, TestJwtTokenService jwtService, ApplicationDbContext dbContext, PasswordHasher<ApplicationUser> hasher) 
        CreateSut(string dbName)
    {
        var db = CreateDbContext(dbName);
        var hasher = new PasswordHasher<ApplicationUser>();
        var jwt = new TestJwtTokenService();
        var jwtOptions = Options.Create(new JwtSettings
        {
            SecretKey = "SuperSecretKeyForTestingPurposesMustBeLongEnough123!",
            Issuer = "PharmaCoreIssuer",
            Audience = "PharmaCoreAudience",
            ExpiryMinutes = 60
        });

        var sut = new AuthService(db, hasher, jwt, jwtOptions);
        return (sut, jwt, db, hasher);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndExpiration()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, jwtService, db, hasher) = CreateSut(dbName);

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Code = "PHARMA-01",
            Name = "Pharma Care Inc",
            SubscriptionEndDate = DateTime.UtcNow.AddYears(1),
            IsDeleted = false
        };
        var role = new Role { Id = 1, Name = AppRoles.Owner };
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantId,
            UserName = "owner@pharma.com",
            Email = "owner@pharma.com",
            NormalizedEmail = "OWNER@PHARMA.COM",
            FullName = "John Owner",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, "SecurePass123!");

        db.Tenants.Add(tenant);
        db.Roles.Add(role);
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            TenantCode = "PHARMA-01",
            Email = "owner@pharma.com",
            Password = "SecurePass123!"
        };

        var result = await sut.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal(jwtService.ExpectedToken, result.AccessToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.NotNull(jwtService.LastPassedUser);
        Assert.Equal(user.Id, jwtService.LastPassedUser.Id);
        Assert.NotNull(jwtService.LastPassedUser.Role);
        Assert.Equal(AppRoles.Owner, jwtService.LastPassedUser.Role.Name);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ThrowsUnauthorizedAccessException_WithGenericMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, _, db, hasher) = CreateSut(dbName);

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Code = "PHARMA-01", Name = "Pharma Care", SubscriptionEndDate = DateTime.UtcNow.AddYears(1), IsDeleted = false };
        var role = new Role { Id = 1, Name = AppRoles.Admin };
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantId,
            UserName = "admin@pharma.com",
            Email = "admin@pharma.com",
            NormalizedEmail = "ADMIN@PHARMA.COM",
            FullName = "Admin User",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, "CorrectPass123!");

        db.Tenants.Add(tenant);
        db.Roles.Add(role);
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            TenantCode = "PHARMA-01",
            Email = "admin@pharma.com",
            Password = "WrongPassword!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
        Assert.Equal("Invalid tenant code, email, or password.", ex.Message);
    }

    [Fact]
    public async Task Login_WithUnknownTenant_ThrowsUnauthorizedAccessException_WithGenericMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, _, _, _) = CreateSut(dbName);

        var request = new LoginRequestDto
        {
            TenantCode = "NON-EXISTENT",
            Email = "user@pharma.com",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
        Assert.Equal("Invalid tenant code, email, or password.", ex.Message);
    }

    [Fact]
    public async Task Login_WithExpiredTenantSubscription_ThrowsUnauthorizedAccessException_WithGenericMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, _, db, hasher) = CreateSut(dbName);

        var tenantId = Guid.NewGuid();
        // Subscription expired yesterday
        var tenant = new Tenant { Id = tenantId, Code = "EXPIRED-TENANT", Name = "Expired Inc", SubscriptionEndDate = DateTime.UtcNow.AddDays(-1), IsDeleted = false };
        var role = new Role { Id = 1, Name = AppRoles.Admin };
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantId,
            UserName = "admin@expired.com",
            Email = "admin@expired.com",
            NormalizedEmail = "ADMIN@EXPIRED.COM",
            FullName = "Admin",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");

        db.Tenants.Add(tenant);
        db.Roles.Add(role);
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            TenantCode = "EXPIRED-TENANT",
            Email = "admin@expired.com",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
        Assert.Equal("Invalid tenant code, email, or password.", ex.Message);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ThrowsUnauthorizedAccessException_WithGenericMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, _, db, hasher) = CreateSut(dbName);

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Code = "PHARMA-01", Name = "Pharma", SubscriptionEndDate = DateTime.UtcNow.AddYears(1), IsDeleted = false };
        var role = new Role { Id = 1, Name = AppRoles.Admin };
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantId,
            UserName = "inactive@pharma.com",
            Email = "inactive@pharma.com",
            NormalizedEmail = "INACTIVE@PHARMA.COM",
            FullName = "Inactive",
            IsActive = false, // Inactive user
            IsDeleted = false,
            RoleId = 1,
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");

        db.Tenants.Add(tenant);
        db.Roles.Add(role);
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            TenantCode = "PHARMA-01",
            Email = "inactive@pharma.com",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
        Assert.Equal("Invalid tenant code, email, or password.", ex.Message);
    }

    [Fact]
    public async Task Login_WithSoftDeletedUser_ThrowsUnauthorizedAccessException_WithGenericMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, _, db, hasher) = CreateSut(dbName);

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Code = "PHARMA-01", Name = "Pharma", SubscriptionEndDate = DateTime.UtcNow.AddYears(1), IsDeleted = false };
        var role = new Role { Id = 1, Name = AppRoles.Admin };
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantId,
            UserName = "deleted@pharma.com",
            Email = "deleted@pharma.com",
            NormalizedEmail = "DELETED@PHARMA.COM",
            FullName = "Deleted",
            IsActive = true,
            IsDeleted = true, // Soft-deleted
            RoleId = 1,
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");

        db.Tenants.Add(tenant);
        db.Roles.Add(role);
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            TenantCode = "PHARMA-01",
            Email = "deleted@pharma.com",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
        Assert.Equal("Invalid tenant code, email, or password.", ex.Message);
    }

    [Fact]
    public async Task Login_CrossTenantAttempt_ThrowsUnauthorizedAccessException_WithGenericMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, _, db, hasher) = CreateSut(dbName);

        var tenantA = new Tenant { Id = Guid.NewGuid(), Code = "TENANT-A", Name = "Tenant A", SubscriptionEndDate = DateTime.UtcNow.AddYears(1), IsDeleted = false };
        var tenantB = new Tenant { Id = Guid.NewGuid(), Code = "TENANT-B", Name = "Tenant B", SubscriptionEndDate = DateTime.UtcNow.AddYears(1), IsDeleted = false };
        var role = new Role { Id = 1, Name = AppRoles.Pharmacist };

        // User belongs to Tenant B
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantB.Id,
            UserName = "user@tenantb.com",
            Email = "user@tenantb.com",
            NormalizedEmail = "USER@TENANTB.COM",
            FullName = "User B",
            IsActive = true,
            IsDeleted = false,
            RoleId = 1,
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");

        db.Tenants.AddRange(tenantA, tenantB);
        db.Roles.Add(role);
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        // Attempts to log in using Tenant A's tenant code
        var request = new LoginRequestDto
        {
            TenantCode = "TENANT-A",
            Email = "user@tenantb.com",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
        Assert.Equal("Invalid tenant code, email, or password.", ex.Message);
    }

    [Fact]
    public async Task Login_WithUserMissingRole_ThrowsUnauthorizedAccessException_WithGenericMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sut, _, db, hasher) = CreateSut(dbName);

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Code = "PHARMA-01", Name = "Pharma", SubscriptionEndDate = DateTime.UtcNow.AddYears(1), IsDeleted = false };
        var user = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantId,
            UserName = "norole@pharma.com",
            Email = "norole@pharma.com",
            NormalizedEmail = "NOROLE@PHARMA.COM",
            FullName = "No Role",
            IsActive = true,
            IsDeleted = false,
            RoleId = 999, // Non-existent role
            Role = null!
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");

        db.Tenants.Add(tenant);
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            TenantCode = "PHARMA-01",
            Email = "norole@pharma.com",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
        Assert.Equal("Invalid tenant code, email, or password.", ex.Message);
    }
}
