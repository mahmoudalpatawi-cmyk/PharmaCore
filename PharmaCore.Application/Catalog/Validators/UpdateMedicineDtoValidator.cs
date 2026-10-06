using FluentValidation;
using PharmaCore.Application.Catalog.DTOs;

namespace PharmaCore.Application.Catalog.Validators;

public class UpdateMedicineDtoValidator : AbstractValidator<UpdateMedicineDto>
{
    public UpdateMedicineDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Medicine name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Barcode)
            .NotEmpty().WithMessage("Barcode is required.")
            .MaximumLength(100);

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price cannot be negative.");
    }
}
