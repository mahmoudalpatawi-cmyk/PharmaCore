using FluentValidation;
using PharmaCore.Application.Inventory.DTOs;

namespace PharmaCore.Application.Inventory.Validators;

public class StockTransferDtoValidator : AbstractValidator<StockTransferDto>
{
    public StockTransferDtoValidator()
    {
        RuleFor(x => x.SourceBatchId).GreaterThan(0);
        RuleFor(x => x.SourceBranchId).GreaterThan(0);
        RuleFor(x => x.DestinationBatchId).GreaterThan(0);
        RuleFor(x => x.DestinationBranchId).GreaterThan(0);

        RuleFor(x => x.SourceBatchId)
            .NotEqual(x => x.DestinationBatchId)
            .WithMessage("Source and destination batches cannot be the same.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Transfer quantity must be greater than zero.");
    }
}
