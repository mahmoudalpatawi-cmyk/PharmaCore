using FluentValidation;
using PharmaCore.Application.Sales.DTOs;

namespace PharmaCore.Application.Sales.Validators;

public class CreateSaleInvoiceDtoValidator : AbstractValidator<CreateSaleInvoiceDto>
{
    public CreateSaleInvoiceDtoValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Branch ID must be greater than zero.");
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage("Shift ID must be greater than zero.");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("Paid amount cannot be negative.");
        
        RuleFor(x => x.CustomerName)
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.BatchId).GreaterThan(0).WithMessage("Batch ID must be greater than zero.");
            items.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            items.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");
            items.RuleFor(i => i.Discount).GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");
        });
    }
}
