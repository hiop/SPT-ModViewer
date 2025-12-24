using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

[ExportTs]
public class SptModResponse
{
    [JsonPropertyName("sptClientMods")]
    public Dictionary<string, List<SPTClientMod>> SptClientMods { get; set; }
    
    [JsonPropertyName("sptServerMods")] 
    public List<SPTServerMod> SptServerMods { get; set; } = new();
    
    [JsonPropertyName("sptForgeMods")] 
    public List<SPTForgeMod> SptForgeMods { get; set; } = new();
}