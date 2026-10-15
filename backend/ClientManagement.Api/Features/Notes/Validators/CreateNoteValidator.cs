
using ClientManagement.Api.Features.Notes.DTOs;
using FluentValidation;

namespace ClientManagement.Api.Features.Notes.Validators;

public class CreateNoteValidator
    : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage(
                "Note content is required and must not exceed 2000 characters.");
    }
}
