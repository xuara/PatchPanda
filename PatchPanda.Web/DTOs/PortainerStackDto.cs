using System.Text.Json.Serialization;

namespace PatchPanda.Web.DTOs;

internal class PortainerStackDto
{
    [JsonPropertyName("Id")]
    internal required int Id { get; set; }

    [JsonPropertyName("Name")]
    internal required string Name { get; set; }

    [JsonPropertyName("Type")]
    internal int Type { get; set; }

    [JsonPropertyName("EndpointId")]
    internal int EndpointId { get; set; }

    [JsonPropertyName("EntryPoint")]
    internal string? EntryPoint { get; set; }

    [JsonPropertyName("ProjectPath")]
    internal string? ProjectPath { get; set; }
}
