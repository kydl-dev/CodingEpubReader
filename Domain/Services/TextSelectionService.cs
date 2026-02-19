using Domain.ValueObjects;
using Shared.Exceptions;

namespace Domain.Services;

/// <summary>
///     Handles the validation and creation of text selections within a chapter,
///     including overlap detection with existing highlights.
/// </summary>
public abstract class TextSelectionService
{
    private const int MaxSelectionLength = 5000;

    /// <summary>
    ///     Creates a validated <see cref="TextRange" /> for a user text selection.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the selection is empty or exceeds the maximum allowed length.</exception>
    public static TextRange CreateSelection(string chapterId, int startOffset, int endOffset, string selectedText)
    {
        if (string.IsNullOrWhiteSpace(chapterId))
            throw new DomainException("ChapterId cannot be empty when creating a text selection.");
        if (startOffset < 0 || endOffset <= startOffset)
            throw new DomainException($"Invalid selection range: start={startOffset}, end={endOffset}.");
        if (string.IsNullOrEmpty(selectedText))
            throw new DomainException("Selected text cannot be empty.");
        if (selectedText.Length > MaxSelectionLength)
            throw new DomainException(
                $"Selection exceeds the maximum allowed length of {MaxSelectionLength} characters.");

        return new TextRange(chapterId, startOffset, endOffset, selectedText);
    }
}