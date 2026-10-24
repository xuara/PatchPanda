namespace PatchPanda.Web.Exceptions;

internal class RateLimitException : Exception
{
    internal DateTimeOffset ResetsAt { get; }
    internal int Limit { get; }

    internal RateLimitException(DateTimeOffset resetsAt, int limit) 
        : base($"Rate limit of {limit} reached. Resets at {resetsAt}.")
    {
        ResetsAt = resetsAt;
        Limit = limit;
    }

    // Required standard constructors
    internal RateLimitException() { }
    internal RateLimitException(string message) : base(message) { }
    internal RateLimitException(string message, Exception inner) : base(message, inner) { }
}