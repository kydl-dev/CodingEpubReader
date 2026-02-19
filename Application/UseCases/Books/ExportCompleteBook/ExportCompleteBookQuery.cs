using MediatR;

namespace Application.UseCases.Books.ExportCompleteBook;

/// <summary>
///     Query to export the complete book as a single HTML document.
///     Useful for creating backups, offline reading, or sharing.
/// </summary>
public record ExportCompleteBookQuery(Guid BookId) : IRequest<string>;