using Application.Exceptions;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.ReadingProgress.UpdateReadingProgress;

public class UpdateReadingProgressCommandHandler(
    IBookRepository bookRepository,
    IReadingProgressRepository progressRepository,
    IReadingHistoryRepository readingHistoryRepository) : IRequestHandler<UpdateReadingProgressCommand>
{
    public async Task Handle(UpdateReadingProgressCommand request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);

        var book = await bookRepository.GetByIdAsync(bookId, cancellationToken)
                   ?? throw new BookNotFoundException(request.BookId);

        var position = new ReadingPosition(bookId, request.ChapterId, request.Progress);
        await progressRepository.SavePositionAsync(position, cancellationToken);

        var history = await readingHistoryRepository.GetByBookIdAsync(bookId, cancellationToken);
        if (history == null)
        {
            var newHistory = ReadingHistory.Create(
                bookId,
                book.Title,
                book.PrimaryAuthor,
                book.Metadata.Isbn);

            newHistory.UpdateLastRead();
            await readingHistoryRepository.AddAsync(newHistory, cancellationToken);
        }
        else
        {
            history.UpdateLastRead();
            await readingHistoryRepository.UpdateAsync(history, cancellationToken);
        }
    }
}