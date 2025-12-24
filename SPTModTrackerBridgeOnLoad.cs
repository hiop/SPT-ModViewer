using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Modding;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SPTModViewer;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "hiop.spt.mod.viewer";
    public override string Name { get; init; } = "SPTModViewer";
    public override string Author { get; init; } = "HioP";
    public override List<string>? Contributors { get; init; }
    public override Version Version { get; init; } = new("0.1.0");
    public override Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
public class SPTModTrackerBridgeOnLoad(
    ISptLogger<SPTModTrackerBridgeOnLoad> logger,
    SPTModViewer sptModViewer
    ) : IOnLoad
{
    public Task OnLoad()
    {
        _ = sptModViewer.PostServerMod();
        logger.Success("[SPTModViewer] loaded!");
        return Task.CompletedTask;
    }
}
