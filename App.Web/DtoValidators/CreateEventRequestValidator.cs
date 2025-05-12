using FluentValidation;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.EventDate)
            .NotEmpty()
            .GreaterThan(DateTimeOffset.UtcNow);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .NotEmpty();
    }
}