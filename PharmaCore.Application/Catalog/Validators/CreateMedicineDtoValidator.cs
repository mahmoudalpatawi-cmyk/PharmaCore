using FluentValidation;
using PharmaCore.Application.Catalog.DTOs;

namespace PharmaCore.Application.Catalog.Validators;

public class CreateMedicineDtoValidator : AbstractValidator<CreateMedicineDto>
{
    public CreateMedicineDtoValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Category ID must be greater than zero.");
        RuleFor(x => x.ManufacturerId).GreaterThan(0).WithMessage("Manufacturer ID must be greater than zero.");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Medicine name is required.")
            .MaximumLength(256).WithMessage("Medicine name cannot exceed 256 characters.");

        RuleFor(x => x.TradeName)
            .NotEmpty().WithMessage("Trade name is required.")
            .MaximumLength(256).WithMessage("Trade name cannot exceed 256 characters.");

        RuleFor(x => x.GenericName)
            .MaximumLength(256).WithMessage("Generic name cannot exceed 256 characters.");

        RuleFor(x => x.DosageForm)
            .NotEmpty().WithMessage("Dosage form is required.")
            .MaximumLength(50).WithMessage("Dosage form cannot exceed 50 characters.");

        RuleFor(x => x.Strength)
            .NotEmpty().WithMessage("Strength is required.")
            .MaximumLength(50).WithMessage("Strength cannot exceed 50 characters.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode cannot exceed 100 characters.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.");
    }
}
