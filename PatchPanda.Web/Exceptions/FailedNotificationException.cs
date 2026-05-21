namespace PatchPanda.Web.Exceptions;

internal class FailedNotificationException : Exception
{
    // Required by CA1032 & CA1515: Standard constructors
    internal FailedNotificationException() { }

    internal FailedNotificationException(string message) : base(message) { }

    internal FailedNotificationException(string message, Exception innerException) 
        : base(message, innerException) { }

    // Named constructor to avoid signature clashing with CA1032 requirements.
    internal static FailedNotificationException ForUrl(string url, Exception inner) 
        => new($"Failed to send notification to {url}", inner);
}