namespace PatchPanda.Web.Exceptions;

internal class FailedNotificationException : Exception
{
    public FailedNotificationException(string notificationUrl, Exception innerException)
        : base($"Failed to send notification to {notificationUrl}", innerException) { }

    // Required by CA1032: Standard parameterless constructor
    public FailedNotificationException() { }

    // Required by CA1032: Standard message constructor
    public FailedNotificationException(string message) : base(message) { }

    // Required by CA1032: Standard message + inner exception constructor
    public FailedNotificationException(string message, Exception innerException) 
        : base(message, innerException) { }
}