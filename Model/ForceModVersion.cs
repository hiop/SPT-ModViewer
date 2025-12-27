using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

[ExportTs]
public class ForceModVersion
{
    [JsonPropertyName("modVersion")] public string ModVersion { get; set; }
    [JsonPropertyName("forceVersion")] public string ForceVersion { get; set; }
}

[ExportTs]
public class ForceModVersionRequest: ForceModVersion
{
    [JsonPropertyName("modType")] public ModType ModType { get; set; }
    
    [JsonPropertyName("clientName")] public string? ClientName { get; set; }
    [JsonPropertyName("guid")] public string Guid { get; set; }
}