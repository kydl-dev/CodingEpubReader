using Application.UseCases.Bookmark.AddBookmark;
using FluentValidation;

namespace Application.Validators;

public class AddBookmarkCommandValidator : AbstractValidator<AddBookmarkCommand>
{
    public AddBookmarkCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required.");

        RuleFor(x => x.ChapterId)
            .NotEmpty()
            .WithMessage("ChapterId is required.");

        RuleFor(x => x.Progress)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage("Progress must be between 0.0 and 1.0.");

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .WithMessage("Note must not exceed 1000 characters.")
            .When(x => x.Note is not null);
    }
}