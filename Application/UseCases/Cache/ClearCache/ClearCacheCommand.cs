using MediatR;

namespace Application.UseCases.Cache.ClearCache;

/// <summary>
///     Command to clear all cached data
/// </summary>
public sealed record ClearCacheCommand : IRequest<Unit>;