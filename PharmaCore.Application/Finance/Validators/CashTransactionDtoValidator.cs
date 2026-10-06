using FluentValidation;
using PharmaCore.Application.Finance.DTOs;

namespace PharmaCore.Application.Finance.Validators;

public class CashTransactionDtoValidator : AbstractValidator<CashTransactionDto>
{
    public CashTransactionDtoValidator()
    {
        RuleFor(x => x.ShiftId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500);
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100);
        RuleFor(x => x.TransactionType).IsInEnum();
    }
}
