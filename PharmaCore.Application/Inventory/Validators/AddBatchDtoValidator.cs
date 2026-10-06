using System;
using FluentValidation;
using PharmaCore.Application.Inventory.DTOs;

namespace PharmaCore.Application.Inventory.Validators;

public class AddBatchDtoValidator : AbstractValidator<AddBatchDto>
{
    public AddBatchDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.MedicineId).GreaterThan(0);
        
        RuleFor(x => x.BatchNumber)
            .NotEmpty().WithMessage("Batch number is required.")
            .MaximumLength(100);

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow.Date).WithMessage("Expiry date must be in the future.");

        RuleFor(x => x.InitialQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Initial quantity cannot be negative.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be zero or positive.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Selling price must be zero or positive.");
    }
}
