using System;
using FluentValidation;
using PharmaCore.Application.Inventory.DTOs;

namespace PharmaCore.Application.Inventory.Validators;

public class AddBatchDtoValidator : AbstractValidator<AddBatchDto>
{
    public AddBatchDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Branch ID must be greater than zero.");
        RuleFor(x => x.MedicineId).GreaterThan(0).WithMessage("Medicine ID must be greater than zero.");
        RuleFor(x => x.SupplierId).GreaterThan(0).When(x => x.SupplierId.HasValue).WithMessage("Supplier ID must be greater than zero if provided.");
        
        RuleFor(x => x.BatchNumber)
            .NotEmpty().WithMessage("Batch number is required.")
            .MaximumLength(100).WithMessage("Batch number cannot exceed 100 characters.");

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
