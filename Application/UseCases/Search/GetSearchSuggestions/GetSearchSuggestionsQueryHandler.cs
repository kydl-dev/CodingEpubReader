using Application.Interfaces;
using MediatR;

namespace Application.UseCases.Search.GetSearchSuggestions;

/// <summary>
///     Handler for retrieving search suggestions for autocomplete.
/// </summary>
public class GetSearchSuggestionsQueryHandler(ISearchService searchService)
    : IRequestHandler<GetSearchSuggestionsQuery, IEnumerable<string>>
{
    private readonly ISearchService _searchService =
        searchService ?? throw new ArgumentNullException(nameof(searchService));

    public async Task<IEnumerable<string>> Handle(
        GetSearchSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        return await _searchService.GetSuggestionsAsync(
            request.BookId,
            request.PartialQuery,
            request.MaxResults,
            cancellationToken);
    }
}