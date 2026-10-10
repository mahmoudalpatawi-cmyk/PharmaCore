using FluentValidation;
using PharmaCore.Application.Purchasing.DTOs;

namespace PharmaCore.Application.Purchasing.Validators;

public class CreatePurchaseInvoiceDtoValidator : AbstractValidator<CreatePurchaseInvoiceDto>
{
    public CreatePurchaseInvoiceDtoValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Supplier ID must be greater than zero.");
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Branch ID must be greater than zero.");
        
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage("Invoice number is required.")
            .MaximumLength(100).WithMessage("Invoice number cannot exceed 100 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.MedicineId).GreaterThan(0).WithMessage("Medicine ID must be greater than zero.");
            items.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            items.RuleFor(i => i.CostPrice).GreaterThanOrEqualTo(0).WithMessage("Cost price cannot be negative.");
            items.RuleFor(i => i.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.");
            items.RuleFor(i => i.BatchNumber).NotEmpty().WithMessage("Batch number is required.");
        });
    }
}
