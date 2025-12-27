using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

[ExportTs]
public class ForceModGuid
{
    [JsonPropertyName("forceGuid")] public string ForceGuid { get; set; }
}

[ExportTs]
public class ForceModGuidRequest: ForceModGuid
{
    [JsonPropertyName("modType")] public ModType ModType { get; set; }
    [JsonPropertyName("clientName")] public string? ClientName { get; set; }
    [JsonPropertyName("guid")] public string Guid { get; set; }
}