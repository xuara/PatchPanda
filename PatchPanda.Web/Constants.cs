namespace PatchPanda.Web;

// CA1515: Application types should be internal unless public API is required
internal static class Constants
{
    // CA2211/CA1805: Non-constant fields shouldn't be visible/redundant null init
    // Changed to PascalCase to satisfy CA1707
    internal static string? BaseUrl;

#if DEBUG
    public const string AppName = "PatchPanda [DEV]";
#else
    public const string AppName = "PatchPanda";
#endif

    public const string DbName = "patchpanda.db";
}

// CA1034: Classes are no longer nested, satisfying "Do not nest type"
internal static class Cascading
{
    public const string Toasts = "TOASTS";
}

internal static class VariableKeys
{
    // The member names follow PascalCase (CA1707), 
    // but the string values remain uppercase to match your environment variables.
    public const string AppriseNotificationUrls = "AppriseNotificationUrls";
    public const string AppriseApiUrl = "AppriseApiUrl";
    public const string DiscordWebhookUrl = "DiscordWebhookUrl";
    public const string BaseUrl = "BaseUrl";
    public const string PortainerUrl = "PortainerUrl";
    public const string PortainerIgnoreSsl = "PortainerIgnoreSsl";
    public const string PortainerAccessToken = "PortainerAccessToken";
    public const string PortainerUsername = "PortainerUsername";
    public const string PortainerPassword = "PortainerPassword";
    public const string OllamaUrl = "OllamaUrl";
    public const string OllamaModel = "OllamaModel";
    public const string OllamaNumCtx = "OllamaNumCtx";
    public const string AppVersion = "AppVersion";
}

internal static class SettingsKeys
{
    public const string AutoUpdateEnabled = "AutoUpdateEnabled";
    public const string AutoUpdateDelayHours = "AutoUpdateDelayHours";
    public const string SecurityScanningEnabled = "SecurityScanningEnabled";
}

internal static class Limits
{
    public const int MaxOllamaAttempts = 3;
    public const int MinimumUpdateSteps = 3;
    public const int PortainerHttpTimeoutSeconds = 60;
    public const int UpdateJobTimeoutSeconds = 300;
}