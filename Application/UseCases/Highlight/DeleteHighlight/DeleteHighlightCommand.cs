using MediatR;

namespace Application.UseCases.Highlight.DeleteHighlight;

public abstract record DeleteHighlightCommand(Guid HighlightId) : IRequest;