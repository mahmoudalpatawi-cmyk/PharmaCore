using FluentValidation;
using PharmaCore.Application.Catalog.DTOs;

namespace PharmaCore.Application.Catalog.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100);
    }
}
