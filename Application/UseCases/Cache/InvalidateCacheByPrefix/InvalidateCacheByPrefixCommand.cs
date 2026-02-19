using MediatR;

namespace Application.UseCases.Cache.InvalidateCacheByPrefix;

/// <summary>
///     Command to invalidate all cache entries matching a prefix
/// </summary>
public sealed record InvalidateCacheByPrefixCommand(string Prefix) : IRequest<Unit>;