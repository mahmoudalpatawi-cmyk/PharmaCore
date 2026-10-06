using FluentValidation;
using PharmaCore.Application.Finance.DTOs;

namespace PharmaCore.Application.Finance.Validators;

public class OpenShiftDtoValidator : AbstractValidator<OpenShiftDto>
{
    public OpenShiftDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.StartingCash).GreaterThanOrEqualTo(0);
    }
}
