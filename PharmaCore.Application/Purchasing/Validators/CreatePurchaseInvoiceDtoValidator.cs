using FluentValidation;
using PharmaCore.Application.Purchasing.DTOs;

namespace PharmaCore.Application.Purchasing.Validators;

public class CreatePurchaseInvoiceDtoValidator : AbstractValidator<CreatePurchaseInvoiceDto>
{
    public CreatePurchaseInvoiceDtoValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.BranchId).GreaterThan(0);
        
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage("Invoice number is required.")
            .MaximumLength(100);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.MedicineId).GreaterThan(0);
            items.RuleFor(i => i.Quantity).GreaterThan(0);
            items.RuleFor(i => i.CostPrice).GreaterThanOrEqualTo(0);
            items.RuleFor(i => i.SellingPrice).GreaterThanOrEqualTo(0);
            items.RuleFor(i => i.BatchNumber).NotEmpty();
        });
    }
}
