namespace PatchPanda.Web.Exceptions;

internal class DockerCommandException : Exception
{
    public int ExitCode { get; }
    public string Command { get; } = string.Empty;
    public string? StdOut { get; }
    public string? StdErr { get; }

    public DockerCommandException(string command, int exitCode, string? stdOut, string? stdErr)
        : base($"Docker compose command {command} failed with exit code {exitCode}")
    {
        Command = command;
        ExitCode = exitCode;
        StdOut = stdOut;
        StdErr = stdErr;
    }

    // Required by CA1032: Standard parameterless constructor
    public DockerCommandException() { }

    // Required by CA1032: Standard message constructor
    public DockerCommandException(string message) : base(message) { }

    // Required by CA1032: Standard message + inner exception constructor
    public DockerCommandException(string message, Exception innerException) 
        : base(message, innerException) { }
}