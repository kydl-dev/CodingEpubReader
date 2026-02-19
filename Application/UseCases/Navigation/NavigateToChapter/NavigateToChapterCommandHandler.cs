using Application.DTOs;
using Application.Exceptions;
using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Navigation.NavigateToChapter;

public class NavigateToChapterCommandHandler(
    IBookRepository bookRepository,
    IReadingProgressRepository progressRepository,
    IReadingHistoryRepository readingHistoryRepository,
    IMapper mapper) : IRequestHandler<NavigateToChapterCommand, ChapterDto>
{
    public async Task<ChapterDto> Handle(
        NavigateToChapterCommand request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);
        var book = await bookRepository.GetByIdAsync(bookId, cancellationToken)
                   ?? throw new BookNotFoundException(request.BookId);

        var chapter = book.GetChapterById(request.ChapterId)
                      ?? throw new ChapterNotFoundException(request.BookId, request.ChapterId);

        // Reset progress to start of the new chapter.
        var position = new ReadingPosition(bookId, request.ChapterId, 0.0);
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

        return mapper.Map<ChapterDto>(chapter);
    }
}