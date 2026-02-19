using Shared.Exceptions;

namespace Application.Exceptions;

public class InvalidEpubFormatException : DomainException
{
    public InvalidEpubFormatException(string filePath, string reason)
        : base($"The file '{filePath}' is not a valid epub: {reason}")
    {
        FilePath = filePath;
    }

    public InvalidEpubFormatException(string filePath, string reason, Exception innerException)
        : base($"The file '{filePath}' is not a valid epub: {reason}", innerException)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}