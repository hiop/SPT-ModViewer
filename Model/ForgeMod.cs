using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

public class ForgeMod
{
    [JsonPropertyName("sptForgeMods")] 
    public List<SPTForgeMod> SptForgeMods { get; set; } = new();
}


[ExportTs]
public class SPTForgeMod
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("guid")] public string Guid { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("teaser")] public string Teaser { get; set; }

    [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; }

    [JsonPropertyName("detail_url")] public string Detail_Url { get; set; }

    [JsonPropertyName("sptVersions")] public List<SPTForgeModVersion> SptVersions { get; set; } = new();
}

[ExportTs]
public class SPTForgeModVersion
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }
    
    [JsonPropertyName("spt_version_constraint")] public string SptVersion { get; set; }
    
    [JsonPropertyName("fika_compatibility")] public string? Fika_Compatibility { get; set; }
}

[ExportTs]
public class ForgeModUpdate
{
    [JsonPropertyName("guid")] public string? Guid { get; set; }
    
    [JsonPropertyName("name")] public string? Name { get; set; }
}
