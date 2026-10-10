using FluentValidation;
using PharmaCore.Application.Transfers.DTOs;

namespace PharmaCore.Application.Transfers.Validators;

public class CreateTransferDocumentDtoValidator : AbstractValidator<CreateTransferDocumentDto>
{
    public CreateTransferDocumentDtoValidator()
    {
        RuleFor(x => x.SourceBranchId).GreaterThan(0).WithMessage("Source branch ID must be greater than zero.");
        RuleFor(x => x.DestinationBranchId).GreaterThan(0).WithMessage("Destination branch ID must be greater than zero.");
        RuleFor(x => x.DestinationBranchId)
            .NotEqual(x => x.SourceBranchId)
            .WithMessage("Destination branch cannot be the same as the source branch.");
            
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.BatchId).GreaterThan(0).WithMessage("Batch ID must be greater than zero.");
            items.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }
}
