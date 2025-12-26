using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

public class ClientMod
{
    [JsonPropertyName("sptClientMods")]
    public Dictionary<string, List<SPTClientMod>> SptClientMods { get; set; } = new();
}

[ExportTs]
public class SPTClientMod
{
    [JsonPropertyName("guid")] public string Guid { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("modVersion")] public string ModVersion { get; set; }
    
    [JsonPropertyName("visible")] public bool? Visible { get; set; }
}

[ExportTs]
public class HideClientMod
{
    [JsonPropertyName("clientName")] public string ClientName { get; set; }
    [JsonPropertyName("guid")] public string Guid { get; set; }
}

[ExportTs]
public class HideServerMod
{
    [JsonPropertyName("guid")] public string Guid { get; set; }
}