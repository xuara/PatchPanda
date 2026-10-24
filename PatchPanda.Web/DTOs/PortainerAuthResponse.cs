using System.Text.Json.Serialization;

namespace PatchPanda.Web.DTOs;

internal class PortainerAuthResponse
{
    [JsonPropertyName("jwt")]
    internal required string Jwt { get; set; }
}
