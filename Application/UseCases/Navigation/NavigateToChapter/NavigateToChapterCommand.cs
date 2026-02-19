using Application.DTOs;
using MediatR;

namespace Application.UseCases.Navigation.NavigateToChapter;

/// <summary>Fetches a specific chapter and saves the new reading position.</summary>
public record NavigateToChapterCommand(Guid BookId, string ChapterId) : IRequest<ChapterDto>;