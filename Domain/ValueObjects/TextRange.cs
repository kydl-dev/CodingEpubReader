namespace Domain.ValueObjects;

public sealed record TextRange
{
    public TextRange(string chapterId, int startOffset, int endOffset, string selectedText)
    {
        if (string.IsNullOrWhiteSpace(chapterId))
            throw new ArgumentException("ChapterId cannot be null or whitespace.", nameof(chapterId));
        if (startOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(startOffset), "StartOffset must be non-negative.");
        if (endOffset < startOffset)
            throw new ArgumentOutOfRangeException(nameof(endOffset),
                "EndOffset must be greater than or equal to StartOffset.");

        ChapterId = chapterId;
        StartOffset = startOffset;
        EndOffset = endOffset;
        SelectedText = selectedText ?? string.Empty;
    }

    public string ChapterId { get; }
    public int StartOffset { get; }
    public int EndOffset { get; }
    public string SelectedText { get; }

    public int Length => EndOffset - StartOffset;

    public bool IsEmpty => Length == 0;
}