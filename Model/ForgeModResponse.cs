using System.Text.Json.Serialization;

namespace SPTModViewer.Config;

public class ForgeModResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("data")] public List<SPTForgeMod?> Data { get; set; }
}