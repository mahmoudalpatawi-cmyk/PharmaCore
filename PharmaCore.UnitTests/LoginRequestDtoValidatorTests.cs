using PharmaCore.Application.Identity.DTOs;
using PharmaCore.Application.Identity.Validators;

namespace PharmaCore.UnitTests;

public class LoginRequestDtoValidatorTests
{
    private readonly LoginRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        var dto = new LoginRequestDto
        {
            TenantCode = "TENANT01",
            Email = "pharmacist@pharmacy.com",
            Password = "SecurePassword123!"
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyTenantCode_FailsValidation(string? tenantCode)
    {
        var dto = new LoginRequestDto
        {
            TenantCode = tenantCode!,
            Email = "user@pharmacy.com",
            Password = "SecurePassword123!"
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequestDto.TenantCode));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    [InlineData("@nodomain.com")]
    [InlineData("plainaddress")]
    public void Validate_InvalidEmail_FailsValidation(string? email)
    {
        var dto = new LoginRequestDto
        {
            TenantCode = "TENANT01",
            Email = email!,
            Password = "SecurePassword123!"
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequestDto.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyPassword_FailsValidation(string? password)
    {
        var dto = new LoginRequestDto
        {
            TenantCode = "TENANT01",
            Email = "user@pharmacy.com",
            Password = password!
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequestDto.Password));
    }
}
