using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
///     Parses an epub file from disk into a fully-constructed <see cref="Book" /> aggregate.
///     Implemented in the Infrastructure layer.
/// </summary>
public interface IEpubParser
{
    /// <summary>
    ///     Parses the epub file at <paramref name="filePath" /> and returns a <see cref="Book" />.
    /// </summary>
    /// <exception cref="Application.Exceptions.InvalidEpubFormatException">
    ///     Thrown when the file is not a valid epub or is missing required sections.
    /// </exception>
    Task<Book> ParseAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns true when the file at <paramref name="filePath" /> appears to be a supported epub.
    ///     Does not fully parse the file — intended for fast pre-import validation.
    /// </summary>
    bool IsSupported(string filePath);
}