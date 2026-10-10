using FluentValidation;
using PharmaCore.Application.Finance.DTOs;

namespace PharmaCore.Application.Finance.Validators;

public class OpenShiftDtoValidator : AbstractValidator<OpenShiftDto>
{
    public OpenShiftDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Branch ID must be greater than zero.");
        RuleFor(x => x.StartingCash).GreaterThanOrEqualTo(0).WithMessage("Starting cash cannot be negative.");
    }
}
