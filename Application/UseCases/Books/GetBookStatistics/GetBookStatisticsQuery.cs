using Application.DTOs.Book;
using MediatR;

namespace Application.UseCases.Books.GetBookStatistics;

/// <summary>
///     Query to retrieve comprehensive statistics about a book
///     (word count, reading time, chapters, etc.)
/// </summary>
public abstract record GetBookStatisticsQuery(Guid BookId) : IRequest<BookStatisticsDto>;