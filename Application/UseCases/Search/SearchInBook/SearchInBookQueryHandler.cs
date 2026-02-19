using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.UseCases.Search.SearchInBook;

public class SearchInBookQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchInBookQuery, IEnumerable<SearchResultDto>>
{
    public async Task<IEnumerable<SearchResultDto>> Handle(
        SearchInBookQuery request, CancellationToken cancellationToken)
    {
        return await searchService.SearchInBookAsync(
            request.BookId,
            request.Query,
            request.CaseSensitive,
            request.WholeWord,
            cancellationToken);
    }
}