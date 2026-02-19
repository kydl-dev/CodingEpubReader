using Application.DTOs;
using Application.DTOs.Book;
using AutoMapper;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── Value Objects ───────────────────────────────────────────────────────
        CreateMap<BookId, Guid>().ConvertUsing(s => s.Value);

        // ── Book ────────────────────────────────────────────────────────────────
        CreateMap<Book, BookDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value));

        CreateMap<Book, BookSummaryDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value))
            .ForMember(d => d.PrimaryAuthor, o => o.MapFrom(s => s.PrimaryAuthor))
            .ForMember(d => d.CoverImagePath, o => o.MapFrom(s => s.Metadata.CoverImagePath))
            .ForMember(d => d.FilePath, o => o.MapFrom(s => s.FilePath))
            // OverallProgress is not stored on Book; the caller enriches it separately.
            .ForMember(d => d.OverallProgress, o => o.Ignore());

        // ── Chapter ─────────────────────────────────────────────────────────────
        CreateMap<Chapter, ChapterDto>();

        // ── Metadata ────────────────────────────────────────────────────────────
        CreateMap<EpubMetadata, MetadataDto>();

        // ── TOC ─────────────────────────────────────────────────────────────────
        CreateMap<TocItem, TocItemDto>();

        // ── Bookmark ────────────────────────────────────────────────────────────
        CreateMap<Bookmark, BookmarkDto>()
            .ForMember(d => d.BookId, o => o.MapFrom(s => s.BookId.Value))
            .ForMember(d => d.ChapterId, o => o.MapFrom(s => s.Position.ChapterId))
            .ForMember(d => d.Progress, o => o.MapFrom(s => s.Position.Progress));

        // ── Highlight ───────────────────────────────────────────────────────────
        CreateMap<Highlight, HighlightDto>()
            .ForMember(d => d.BookId, o => o.MapFrom(s => s.BookId.Value))
            .ForMember(d => d.ChapterId, o => o.MapFrom(s => s.TextRange.ChapterId))
            .ForMember(d => d.StartOffset, o => o.MapFrom(s => s.TextRange.StartOffset))
            .ForMember(d => d.EndOffset, o => o.MapFrom(s => s.TextRange.EndOffset))
            .ForMember(d => d.SelectedText, o => o.MapFrom(s => s.TextRange.SelectedText));

        // ── ReadingPosition ─────────────────────────────────────────────────────
        CreateMap<ReadingPosition, ReadingPositionDto>()
            .ForMember(d => d.BookId, o => o.MapFrom(s => s.BookId.Value));

        // ── SearchResult (Domain VO → DTO) ──────────────────────────────────────
        CreateMap<SearchResult, SearchResultDto>();
    }
}