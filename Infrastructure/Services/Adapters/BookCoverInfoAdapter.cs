using Domain.Entities;
using Shared.BackgroundWorkers.Utils;

namespace Infrastructure.Services.Adapters;

/// <summary>
///     Adapter to make Domain.Book implement IBookCoverInfo
/// </summary>
internal class BookCoverInfoAdapter(Book book) : IBookCoverInfo
{
    private readonly Book _book = book ?? throw new ArgumentNullException(nameof(book));

    public Guid Id => _book.Id.Value;

    public string Title => _book.Title;

    public string GetCoverImagePath()
    {
        return _book.Metadata.CoverImagePath ?? string.Empty;
    }
}