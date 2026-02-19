using System.Text.RegularExpressions;
using Application.UseCases.Highlight.AddHighlight;
using FluentValidation;

namespace Application.Validators;

public class AddHighlightCommandValidator : AbstractValidator<AddHighlightCommand>
{
    // Matches #RGB, #RRGGBB, #RRGGBBAA
    private static readonly Regex HexColorRegex =
        new("^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
            RegexOptions.Compiled);

    public AddHighlightCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required.");

        RuleFor(x => x.ChapterId)
            .NotEmpty()
            .WithMessage("ChapterId is required.");

        RuleFor(x => x.SelectedText)
            .NotEmpty()
            .WithMessage("Selected text cannot be empty.")
            .MaximumLength(5000)
            .WithMessage("Selected text must not exceed 5000 characters.");

        RuleFor(x => x.StartOffset)
            .GreaterThanOrEqualTo(0)
            .WithMessage("StartOffset must be non-negative.");

        RuleFor(x => x.EndOffset)
            .GreaterThan(x => x.StartOffset)
            .WithMessage("EndOffset must be greater than StartOffset.");

        RuleFor(x => x.Color)
            .NotEmpty()
            .WithMessage("Color is required.")
            .Matches(HexColorRegex)
            .WithMessage("Color must be a valid hex color string (e.g. #FFFF00).");

        RuleFor(x => x.Note)
            .MaximumLength(2000)
            .WithMessage("Note must not exceed 2000 characters.")
            .When(x => x.Note is not null);
    }
}