using System.Text.Json.Serialization;
using SPT.BridgeUI.Core.Attributes;

namespace SPTModViewer.Config;

[ExportTs]
public class ServerDataResponse
{
    [JsonPropertyName("sptServerVersion")] public string SptServerVersion{ get; set; }
}