using System.Text.Json.Serialization;

namespace SPTModViewer.Config;

public class ForgeResponse<T>
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("data")] public List<T> Data { get; set; }
}