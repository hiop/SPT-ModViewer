using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

[ExportTs]
public class RemoveModRequest
{
    [JsonPropertyName("modType")] public required ModType ModType { get; set; }
    
    [JsonPropertyName("clientName")] public string? ClientName { get; set; }
    
    [JsonPropertyName("guid")] public required string Guid { get; set; }
}