using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

[ExportTs]
public class ApiResponse<T>
{
    [JsonPropertyName("success")] public bool Success { get; set; } = true;
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("data")] public T? Data { get; set; }
}