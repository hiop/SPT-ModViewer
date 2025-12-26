using System.Text.Json.Serialization;

namespace SPTModViewer.Config;

public class SPTModViewerConfig
{
    [JsonPropertyName("forgeApiToken")] 
    public string ForgeApiToken { get; set; }
}