using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PatchPanda.Web.DTOs;

namespace PatchPanda.Web.Services;

internal class PortainerService : IPortainerService, IDisposable
{
    private readonly HttpClient? _httpClient;
    private readonly ILogger<PortainerService> _logger;
    private readonly string? _url;
    private readonly string? _accessToken;
    private readonly string? _username;
    private readonly string? _password;
    private string? _jwt;
    private DateTime? _jwtExpiry;

    public void Dispose() => _httpClient?.Dispose();

    public bool IsConfigured =>
        IsUrlConfigured
        && (IsAccessTokenConfigured || IsUsernamePasswordConfigured);

    private bool IsUrlConfigured =>
        !string.IsNullOrWhiteSpace(_url);

    public bool IsAccessTokenConfigured =>
        !string.IsNullOrWhiteSpace(_accessToken);

    private bool IsUsernamePasswordConfigured =>
        !string.IsNullOrWhiteSpace(_username)
        && !string.IsNullOrWhiteSpace(_password);

    internal PortainerService(
        IConfiguration configuration,
        ILogger<PortainerService> logger
    )
    {
        _logger = logger;
        _url = configuration.GetValue<string?>(VariableKeys.PortainerUrl) ?? Environment.GetEnvironmentVariable("PORTAINER_URL");
        _accessToken = configuration.GetValue<string?>(VariableKeys.PortainerAccessToken) ?? Environment.GetEnvironmentVariable("PORTAINER_ACCESS_TOKEN");
        var ignoreSsl = configuration.GetValue<bool>(VariableKeys.PortainerIgnoreSsl) || (Environment.GetEnvironmentVariable("PORTAINER_IGNORE_SSL") == "true");
        _username = configuration.GetValue<string?>(VariableKeys.PortainerUsername) ?? Environment.GetEnvironmentVariable("PORTAINER_USERNAME");
        _password = configuration.GetValue<string?>(VariableKeys.PortainerPassword) ?? Environment.GetEnvironmentVariable("PORTAINER_PASSWORD");
        var timeoutSeconds = Limits.PortainerHttpTimeoutSeconds;

        if (!IsUrlConfigured)
        {
            logger.LogWarning("{Url} is missing. Please provide it and an authentication method if you wish to enable Portainer integration.", VariableKeys.PortainerUrl);
            return;
        }

        if (!IsConfigured)
        {
            logger.LogWarning(
                "Portainer authentication is missing. Please provide {AccessToken} or {Username} and {Password} if you wish to enable Portainer integration.",
                VariableKeys.PortainerAccessToken,
                VariableKeys.PortainerUsername,
                VariableKeys.PortainerPassword
            );
            return;
        }

        using var handler = new HttpClientHandler();
        handler.CheckCertificateRevocationList = true;

        if (ignoreSsl)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            logger.LogWarning(
                "SSL certificate validation is disabled for Portainer. This is insecure and should only be used for self-signed certificates in trusted environments."
            );
        }

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_url!),
            Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds))
        };

        if (IsAccessTokenConfigured)
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _accessToken);
            logger.LogInformation("PortainerService using access token authentication.");
        }
        logger.LogInformation("PortainerService initialized with URL: {Url}", _url);
    }

    public async Task<bool> ValidateAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !IsAccessTokenConfigured || _httpClient is null)
            return false;

        try
        {
            var response = await _httpClient.GetAsync(new Uri("/api/motd", UriKind.Relative), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Portainer access token validation successful.");
                return true;
            }

            _logger.LogWarning(
                "Portainer access token validation failed with status {Status}. Check that {AccessToken} is a valid token.",
                response.StatusCode,
                VariableKeys.PortainerAccessToken
            );
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Exception occurred while validating access token. Check that {Url} is reachable.",
                VariableKeys.PortainerUrl
            );
            return false;
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (!IsUrlConfigured || IsAccessTokenConfigured || !IsUsernamePasswordConfigured || _httpClient is null)
            return;

        if (
            !string.IsNullOrWhiteSpace(_jwt)
            && _jwtExpiry is not null
            && _jwtExpiry > DateTime.UtcNow
        )
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _jwt
            );
            return;
        }

        var payload = JsonSerializer.Serialize(new { username = _username, password = _password });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var resp = await _httpClient.PostAsync(
            new Uri("/api/auth", UriKind.Relative),
            content,
            cancellationToken
        );

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Portainer authentication failed with status {Status}. Make sure {Url}, {Username} and {Password} are set correctly.",
                resp.StatusCode,
                VariableKeys.PortainerUrl,
                VariableKeys.PortainerUsername,
                VariableKeys.PortainerPassword
            );
            return;
        }

        var json = await resp.Content.ReadFromJsonAsync<PortainerAuthResponse>(
            cancellationToken: cancellationToken
        );

        if (json?.Jwt is not null)
        {
            _jwt = json.Jwt;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _jwt
            );
            _jwtExpiry = DateTime.UtcNow.AddHours(8);
        }
        else
            _logger.LogWarning("Failed parsing Portainer auth response");
    }

    private async Task<PortainerStackDto?> GetStack(
        string stackName,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConfigured || _httpClient is null)
            return null;

        await EnsureAuthenticatedAsync(cancellationToken);

        var filters = JsonSerializer.Serialize(new { Name = stackName });
        var resp = await _httpClient.GetAsync(
            new Uri($"/api/stacks?filters={filters}", UriKind.Relative),
            cancellationToken
        );

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Could not list Portainer stacks: {Status}. Check that {UrlKey} is reachable and credentials are valid",
                resp.StatusCode,
                VariableKeys.PortainerUrl
            );
            return null;
        }

        var stacks = await resp.Content.ReadFromJsonAsync<PortainerStackDto[]>(
            cancellationToken: cancellationToken
        );

        if (stacks is null || stacks.Length == 0)
        {
            _logger.LogWarning(
                "No Portainer stacks found when searching with filters: {Filters}",
                filters
            );
            return null;
        }

        return stacks[0];
    }

    public async Task<string?> GetStackFileContentAsync(
        string stackName,
        CancellationToken cancellationToken = default
    )
    {
        var first = await GetStack(stackName, cancellationToken);

        if (first is null)
            return null;

        var fileResp = await _httpClient!.GetAsync(
            new Uri($"/api/stacks/{first.Id}/file?endpointId={first.EndpointId}", UriKind.Relative),
            cancellationToken
        );

        if (!fileResp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Could not get Portainer stack file: {Status}. Ensure {UrlKey} and credentials are valid and stack exists.",
                fileResp.StatusCode,
                VariableKeys.PortainerUrl
            );
            return null;
        }

        var fileDto = await fileResp.Content.ReadFromJsonAsync<PortainerStackFileDto>(
            cancellationToken: cancellationToken
        );
        return fileDto?.StackFileContent;
    }

    public async Task UpdateStackFileContentAsync(
        string stackName,
        string newFileContent,
        CancellationToken cancellationToken = default
    )
    {
        var first = await GetStack(stackName, cancellationToken) ?? throw new InvalidOperationException("No such stack found.");


        var payload = JsonSerializer.Serialize(
            new { stackFileContent = newFileContent, pullImage = true }
        );
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var putResp = await _httpClient!.PutAsync(
            new Uri($"/api/stacks/{first.Id}?endpointId={first.EndpointId}", UriKind.Relative),
            content,
            cancellationToken
        );

        if (!putResp.IsSuccessStatusCode)
            throw new(
                $"Could not update Portainer stack file: {putResp.StatusCode}, full response: {await putResp.Content.ReadAsStringAsync(cancellationToken)}. Check {VariableKeys.PortainerUrl} and credentials"
            );
    }
}
