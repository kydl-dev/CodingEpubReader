using Application.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.UseCases.Bookmark.AddBookmark;

public abstract record AddBookmarkCommand(
    Guid BookId,
    string ChapterId,
    double Progress,
    BookmarkType Type,
    string? Note = null) : IRequest<BookmarkDto>;