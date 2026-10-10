using FluentValidation;
using PharmaCore.Application.Identity.DTOs;

namespace PharmaCore.Application.Identity.Validators;

/// <summary>
/// FluentValidation validator for LoginRequestDto.
/// </summary>
public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.TenantCode)
            .NotEmpty().WithMessage("Tenant code is required.")
            .MaximumLength(50).WithMessage("Tenant code cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
