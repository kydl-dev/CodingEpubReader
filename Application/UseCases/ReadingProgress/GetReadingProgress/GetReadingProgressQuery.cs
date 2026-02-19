using Application.DTOs;
using MediatR;

namespace Application.UseCases.ReadingProgress.GetReadingProgress;

public sealed record GetReadingProgressQuery(Guid BookId) : IRequest<ReadingPositionDto?>;