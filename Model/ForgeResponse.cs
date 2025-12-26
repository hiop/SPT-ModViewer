using System.Text.Json.Serialization;

namespace SPTModViewer.Config;

public class ForgeResponse<T>
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("data")] public List<T> Data { get; set; }
    
    [JsonPropertyName("meta")] public ForgeResponseMeta Meta { get; set; }
}

public class ForgeResponseMeta
{
    [JsonPropertyName("last_page")] public int LastPage { get; set; }
}
