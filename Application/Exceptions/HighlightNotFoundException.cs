using Shared.Exceptions;

namespace Application.Exceptions;

public class HighlightNotFoundException(Guid id) : DomainException($"Highlight with Id '{id}' was not found.")
{
    public Guid Id { get; } = id;
}