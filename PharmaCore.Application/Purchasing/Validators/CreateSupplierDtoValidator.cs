using FluentValidation;
using PharmaCore.Application.Purchasing.DTOs;

namespace PharmaCore.Application.Purchasing.Validators;

public class CreateSupplierDtoValidator : AbstractValidator<CreateSupplierDto>
{
    public CreateSupplierDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Supplier name is required.")
            .MaximumLength(200);
    }
}
