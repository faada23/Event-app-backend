using FluentValidation;

public class EventFilterCriteriaRequestValidator : AbstractValidator<EventFilterCriteriaRequest>
{
    public EventFilterCriteriaRequestValidator()
    {
        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);

        RuleFor(x => x.Location)
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.CategoryName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.CategoryName));

        RuleFor(x => x.EventName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.EventName));
    }
}