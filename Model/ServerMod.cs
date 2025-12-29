using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SPTModViewer.Config;

public class ServerMod
{
    [JsonPropertyName("sptServerMods")] 
    public List<SPTServerMod> SptServerMods { get; set; } = new();
}

[ExportTs]
public class SPTServerMod
{
    [JsonPropertyName("guid")] 
    public string Guid { get; set; }
    
    [JsonPropertyName("forceGuid")] 
    public string? ForceGuid { get; set; }
    
    [JsonPropertyName("name")] 
    public string Name { get; set; }
    
    [JsonPropertyName("author")] 
    public string Author { get; set; }

    [JsonPropertyName("modVersion")] 
    public string ModVersion { get; set; }    
    
    [JsonPropertyName("forceModVersion")] 
    public ForceModVersion? ForceModVersion { get; set; }
    
    [JsonPropertyName("visible")] public bool? Visible { get; set; }

    [JsonPropertyName("uninstalled")] public bool? Uninstalled { get; set; } = false;
    
    [JsonPropertyName("sptVersion")] 
    public string SptVersion { get; set; }
}