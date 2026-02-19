using Application.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.UseCases.Highlight.DeleteHighlight;

public class DeleteHighlightCommandHandler(
    IHighlightRepository highlightRepository) : IRequestHandler<DeleteHighlightCommand>
{
    public async Task Handle(DeleteHighlightCommand request, CancellationToken cancellationToken)
    {
        var highlight = await highlightRepository.GetByIdAsync(request.HighlightId, cancellationToken)
                        ?? throw new HighlightNotFoundException(request.HighlightId);

        await highlightRepository.DeleteAsync(highlight.Id, cancellationToken);
    }
}