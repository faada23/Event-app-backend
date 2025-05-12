using FluentValidation;

public class CreateUpdateCategoryRequestValidator : AbstractValidator<CreateUpdateCategoryRequest>
{
    public CreateUpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}