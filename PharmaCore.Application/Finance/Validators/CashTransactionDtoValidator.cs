using FluentValidation;
using PharmaCore.Application.Finance.DTOs;

namespace PharmaCore.Application.Finance.Validators;

public class CashTransactionDtoValidator : AbstractValidator<CashTransactionDto>
{
    public CashTransactionDtoValidator()
    {
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage("Shift ID must be greater than zero.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");
        RuleFor(x => x.TransactionType).IsInEnum().WithMessage("Invalid transaction type.");
    }
}
