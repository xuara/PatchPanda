using System.Text.Json.Serialization;

namespace PatchPanda.Web.DTOs;

internal class PortainerStackFileDto
{
    [JsonPropertyName("StackFileContent")]
    internal required string StackFileContent { get; set; }
}
