
using ClientManagement.Api.Features.FollowUps.DTOs;
using FluentValidation;

namespace ClientManagement.Api.Features.FollowUps.Validators;

public class CreateFollowUpValidator
    : AbstractValidator<CreateFollowUpRequest>
{
    public CreateFollowUpValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.DueDate)
            .Must(date => date != default)
            .WithMessage("Due date is required.");
    }
}
