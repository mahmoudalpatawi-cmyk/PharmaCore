using FluentValidation;
using PharmaCore.Application.Catalog.DTOs;

namespace PharmaCore.Application.Catalog.Validators;

public class UpdateMedicineDtoValidator : AbstractValidator<UpdateMedicineDto>
{
    public UpdateMedicineDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Medicine ID must be greater than zero.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category ID must be greater than zero.");

        RuleFor(x => x.ManufacturerId)
            .GreaterThan(0).WithMessage("Manufacturer ID must be greater than zero.");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Medicine name is required.")
            .MaximumLength(256).WithMessage("Medicine name cannot exceed 256 characters.");

        RuleFor(x => x.TradeName)
            .NotEmpty().WithMessage("Trade name is required.")
            .MaximumLength(256).WithMessage("Trade name cannot exceed 256 characters.");

        RuleFor(x => x.GenericName)
            .NotEmpty().WithMessage("Generic name is required.")
            .MaximumLength(256).WithMessage("Generic name cannot exceed 256 characters.");

        RuleFor(x => x.DosageForm)
            .NotEmpty().WithMessage("Dosage form is required.")
            .MaximumLength(50).WithMessage("Dosage form cannot exceed 50 characters.");

        RuleFor(x => x.Strength)
            .NotEmpty().WithMessage("Strength is required.")
            .MaximumLength(50).WithMessage("Strength cannot exceed 50 characters.");

        RuleFor(x => x.PrimaryUnit)
            .NotEmpty().WithMessage("Primary unit is required.")
            .MaximumLength(50).WithMessage("Primary unit cannot exceed 50 characters.");

        RuleFor(x => x.SecondaryUnit)
            .MaximumLength(50).WithMessage("Secondary unit cannot exceed 50 characters.");

        RuleFor(x => x.TertiaryUnit)
            .MaximumLength(50).WithMessage("Tertiary unit cannot exceed 50 characters.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode cannot exceed 100 characters.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative.");

        RuleFor(x => x.PrimaryToSecondaryConversionFactor)
            .GreaterThan(0).WithMessage("Primary to secondary conversion factor must be greater than zero.");

        RuleFor(x => x.SecondaryToTertiaryConversionFactor)
            .GreaterThan(0).WithMessage("Secondary to tertiary conversion factor must be greater than zero.");
    }
}
