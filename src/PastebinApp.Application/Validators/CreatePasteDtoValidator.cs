using FluentValidation;
using PastebinApp.Application.DTOs;

namespace PastebinApp.Application.Validators;

public sealed class CreatePasteDtoValidator : AbstractValidator<CreatePasteDto>
{
    private const int MaxContentLengthChars = 524_288; // 512 KB (chars)
    private const int MinExpirationHours = 1;
    private const int MaxLanguageLength = 50;
    private const int MaxTitleLength = 200;

    public CreatePasteDtoValidator()
    {
        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(MaxContentLengthChars)
            .WithMessage($"Content must be between 1 and {MaxContentLengthChars} characters (512 KB)");

        RuleFor(x => x.ExpirationHours)
            .GreaterThanOrEqualTo(MinExpirationHours)
            .WithMessage($"Expiration hours must be greater than or equal to {MinExpirationHours}");

        RuleFor(x => x.Language)
            .MaximumLength(MaxLanguageLength)
            .WithMessage($"Language cannot exceed {MaxLanguageLength} characters")
            .When(x => x.Language is not null);

        RuleFor(x => x.Title)
            .MaximumLength(MaxTitleLength)
            .WithMessage($"Title cannot exceed {MaxTitleLength} characters")
            .When(x => x.Title is not null);
    }
}