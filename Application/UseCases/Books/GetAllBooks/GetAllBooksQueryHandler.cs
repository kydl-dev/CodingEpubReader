using Application.DTOs.Book;
using AutoMapper;
using Domain.Repositories;
using Domain.Services;
using MediatR;

namespace Application.UseCases.Books.GetAllBooks;

public class GetAllBooksQueryHandler(
    IBookRepository bookRepository,
    IReadingProgressRepository progressRepository,
    IMapper mapper) : IRequestHandler<GetAllBooksQuery, IEnumerable<BookSummaryDto>>
{
    public async Task<IEnumerable<BookSummaryDto>> Handle(
        GetAllBooksQuery request, CancellationToken cancellationToken)
    {
        var books = await bookRepository.GetAllAsync(cancellationToken);
        var result = new List<BookSummaryDto>();

        foreach (var book in books)
        {
            var summary = mapper.Map<BookSummaryDto>(book);

            // Enrich with reading progress (best-effort; default to 0 on missing data).
            var position = await progressRepository.GetLastPositionAsync(book.Id, cancellationToken);
            var overallProgress = position is not null
                ? ReadingProgressCalculator.CalculateOverallProgress(book, position)
                : 0.0;

            summary.OverallProgress = overallProgress;
            result.Add(summary);
        }

        return result.OrderByDescending(b => b.LastOpenedDate).ThenBy(b => b.Title);
    }
}