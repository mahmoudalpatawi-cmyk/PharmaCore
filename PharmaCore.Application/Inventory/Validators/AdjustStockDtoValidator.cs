using FluentValidation;
using PharmaCore.Application.Inventory.DTOs;

namespace PharmaCore.Application.Inventory.Validators;

public class AdjustStockDtoValidator : AbstractValidator<AdjustStockDto>
{
    public AdjustStockDtoValidator()
    {
        RuleFor(x => x.BatchId).GreaterThan(0);
        
        RuleFor(x => x.NewQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Adjusted quantity cannot be negative.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("An adjustment reason is required.")
            .MinimumLength(5);
    }
}
