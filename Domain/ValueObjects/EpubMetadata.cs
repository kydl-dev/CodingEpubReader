using Domain.Enums;

namespace Domain.ValueObjects;

public sealed record EpubMetadata
{
    // Parameterless constructor for EF Core
    private EpubMetadata()
    {
        Creators = new List<string>().AsReadOnly();
    }

    public EpubMetadata(
        string? publisher = null,
        string? description = null,
        IEnumerable<string>? creators = null,
        string? isbn = null,
        string? googleBooksId = null,
        string? calibreId = null,
        string? uuid = null,
        string? subject = null,
        string? rights = null,
        DateTime? publishedDate = null,
        string? coverImagePath = null,
        BookFormat format = BookFormat.Epub,
        string? epubVersion = null)
    {
        Publisher = publisher;
        Description = description;
        Creators = creators?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        Isbn = isbn;
        GoogleBooksId = googleBooksId;
        CalibreId = calibreId;
        Uuid = uuid;
        Subject = subject;
        Rights = rights;
        PublishedDate = publishedDate;
        CoverImagePath = coverImagePath;
        Format = format;
        EpubVersion = epubVersion;
    }

    public string? Publisher { get; init; }
    public string? Description { get; init; }

    /// <summary>
    ///     All creators listed in the epub manifest (authors, editors, illustrators, etc.).
    /// </summary>
    public IReadOnlyList<string> Creators { get; init; }

    public string? Isbn { get; init; }

    /// <summary>Google Books volume ID — absent when the book has not been matched.</summary>
    public string? GoogleBooksId { get; init; }

    /// <summary>Calibre database ID — absent when the book was not imported via Calibre.</summary>
    public string? CalibreId { get; init; }

    /// <summary>The epub package unique-identifier UUID — absent when not provided by the epub.</summary>
    public string? Uuid { get; init; }

    public string? Subject { get; init; }
    public string? Rights { get; init; }
    public DateTime? PublishedDate { get; init; }
    public string? CoverImagePath { get; init; }
    public BookFormat Format { get; init; }
    public string? EpubVersion { get; init; }

    public static EpubMetadata Empty => new();

    public bool HasCover => !string.IsNullOrWhiteSpace(CoverImagePath);
    public bool HasGoogleBooksId => !string.IsNullOrWhiteSpace(GoogleBooksId);
    public bool HasCalibreId => !string.IsNullOrWhiteSpace(CalibreId);
    public bool HasUuid => !string.IsNullOrWhiteSpace(Uuid);
}