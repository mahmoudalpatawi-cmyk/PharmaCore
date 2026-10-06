using FluentValidation;
using PharmaCore.Application.Sales.DTOs;

namespace PharmaCore.Application.Sales.Validators;

public class CreateSaleInvoiceDtoValidator : AbstractValidator<CreateSaleInvoiceDto>
{
    public CreateSaleInvoiceDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ShiftId).GreaterThan(0);
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
        
        RuleFor(x => x.CustomerName)
            .MaximumLength(200);

        RuleFor(x => x.Items)
            .NotNull()
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.BatchId).GreaterThan(0);
            items.RuleFor(i => i.Quantity).GreaterThan(0);
            items.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
            items.RuleFor(i => i.Discount).GreaterThanOrEqualTo(0);
        });
    }
}
