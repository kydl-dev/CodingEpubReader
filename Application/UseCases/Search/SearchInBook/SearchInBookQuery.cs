using Application.DTOs;
using MediatR;

namespace Application.UseCases.Search.SearchInBook;

public sealed record SearchInBookQuery(
    Guid BookId,
    string Query,
    bool CaseSensitive = false,
    bool WholeWord = false) : IRequest<IEnumerable<SearchResultDto>>;