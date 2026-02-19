using MediatR;

namespace Application.UseCases.Search.GetSearchSuggestions;

/// <summary>
///     Query to get search suggestions based on partial input for autocomplete functionality.
/// </summary>
public record GetSearchSuggestionsQuery(
    Guid BookId,
    string PartialQuery,
    int MaxResults = 10) : IRequest<IEnumerable<string>>;