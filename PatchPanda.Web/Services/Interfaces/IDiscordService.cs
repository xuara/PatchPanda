namespace PatchPanda.Web.Services.Interfaces;

internal interface IDiscordService
{
    public string? WebhookUrl { get; }
    public bool IsInitialized { get; }
    public Task SendRawAsync(string content);
}
