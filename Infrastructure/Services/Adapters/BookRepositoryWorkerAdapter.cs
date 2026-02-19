using Domain.Repositories;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Utils;

namespace Infrastructure.Services.Adapters;

/// <summary>
///     Adapter to make Domain.IBookRepositoryWorker implement the worker's IBookRepositoryWorker interface
/// </summary>
public class BookRepositoryWorkerAdapter(IBookRepository bookRepository) : IBookRepositoryWorker
{
    private readonly IBookRepository _bookRepository =
        bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));

    public async Task<IEnumerable<IBookCoverInfo>> GetBooksNeedingThumbnailsAsync(
        CancellationToken cancellationToken = default)
    {
        // Get all books and wrap them in the IBookCoverInfo interface
        var books = await _bookRepository.GetAllAsync(cancellationToken);
        return books.Select(b => new BookCoverInfoAdapter(b)).ToList();
    }
}