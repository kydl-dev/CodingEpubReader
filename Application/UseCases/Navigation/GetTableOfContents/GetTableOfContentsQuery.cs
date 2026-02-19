using Application.DTOs;
using MediatR;

namespace Application.UseCases.Navigation.GetTableOfContents;

public record GetTableOfContentsQuery(Guid BookId) : IRequest<IEnumerable<TocItemDto>>;