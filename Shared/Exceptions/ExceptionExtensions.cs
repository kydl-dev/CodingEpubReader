namespace Shared.Exceptions;

public static class ExceptionExtensions
{
    /// <summary>
    ///     Returns a fully unwrapped, human-readable message for an exception,
    ///     recursively including all inner exception messages.
    ///     <para>
    ///         <see cref="AggregateException" /> inner exceptions are each unwrapped and
    ///         concatenated in brackets. For all other exception types, inner exception
    ///         messages are appended inline so the full causal chain is visible at a glance.
    ///     </para>
    /// </summary>
    public static string FullMessage(this Exception ex)
    {
        if (ex is AggregateException aex)
            return aex.InnerExceptions
                .Aggregate("[ ", (total, next) => $"{total}[{next.FullMessage()}] ") + "]";

        var msg = ex.Message.Replace(", see inner exception.", "").Trim();
        var innerMsg = ex.InnerException?.FullMessage();

        if (innerMsg is not null && innerMsg != msg)
            msg = $"{msg} [ {innerMsg} ]";

        return msg;
    }
}