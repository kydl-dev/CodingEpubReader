using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Application.Common.Behaviors;

/// <summary>
///     MediatR pipeline behavior that logs request entry, success, and failure
///     for every command and query that passes through the pipeline.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {RequestName}", RequestName);

        try
        {
            var response = await next(cancellationToken);
            logger.LogInformation("Handled {RequestName} successfully", RequestName);
            return response;
        }
        catch (DomainException ex)
        {
            // Domain / application exceptions are expected; log as warning without stack trace.
            logger.LogWarning("Domain exception in {RequestName}: {Message}", RequestName, ex.FullMessage());
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected Infrastructure or runtime exception; log full details.
            logger.LogError(ex, "Unhandled exception in {RequestName}: {Message}", RequestName, ex.FullMessage());
            throw;
        }
    }
}