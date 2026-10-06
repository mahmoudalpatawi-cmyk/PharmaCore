using FluentValidation;
using PharmaCore.Application.Transfers.DTOs;

namespace PharmaCore.Application.Transfers.Validators;

public class CreateTransferDocumentDtoValidator : AbstractValidator<CreateTransferDocumentDto>
{
    public CreateTransferDocumentDtoValidator()
    {
        RuleFor(x => x.SourceBranchId).GreaterThan(0);
        RuleFor(x => x.DestinationBranchId).GreaterThan(0);
        RuleFor(x => x.DestinationBranchId)
            .NotEqual(x => x.SourceBranchId)
            .WithMessage("Destination branch cannot be the same as the source branch.");
            
        RuleFor(x => x.Items)
            .NotNull()
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.BatchId).GreaterThan(0);
            items.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
