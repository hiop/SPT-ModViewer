using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using SPTModViewer.Config;
using SPT.BridgeUI.Core;
using SPT.BridgeUI.Core.Attributes;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Services;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Modding;

namespace SPTModViewer;

[Injectable(TypePriority = 0)]
public class SPTModViewer : WebUiModBase
{
    private static readonly string _dataPath = @"user\mods\SPTModViewer\Data\";
    private static readonly string _activeClientModsPath = _dataPath + "ActiveClientMods.json5";
    private static readonly string _serverMods = _dataPath + "ServerMods.json5";
    private static readonly string _forgeMods = _dataPath + "ForgeMods.json5";
    
    private static readonly HttpClient client = new HttpClient();
    private static ProfileHelper _profileHelper;
    private static ProfileActivityService _profileActivityService;
    private static ISptLogger<SPTModViewer> _logger;

    // URL path for your mod's web UI (e.g., https://127.0.0.1:6969/mymod/)
    protected override string BasePath => "/smt";

    public SPTModViewer(ModHelper modHelper, ISptLogger<SPTModViewer> logger, ProfileActivityService profileActivityService, ProfileHelper profileHelper)
        : base(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()))
    {
        _profileHelper = profileHelper;
        _logger = logger;
        _profileActivityService = profileActivityService;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "ZhKBDIAOj7DeT2yC33qnJaShNKpkm5QJuVR2LBuG383dd4a9");
        
        
        logger.Success("Checking version..."+ProgramStatics.SPT_VERSION());
    }

    [ApiEndpoint("/smt/api/forge-mod", "POST", Name="updateModByForge", Description = "Update mod with forge if possible")]
    public async Task<ApiResponse<SPTForgeMod>> UpdateModByForge(ForgeModUpdate update)
    {
        SPTForgeMod? mod = null;
        
        if (update?.Guid != null)
        {
            var response = await FindModDataByGuid(update.Guid);
            if (response != null)
            {
                mod = response.Data.FirstOrDefault();
            }
        }

        if (mod == null && update?.Name != null)
        {
            var response = await FindModDataByName(update.Name);
            if (response != null)
            {
                mod = response.Data.FirstOrDefault();
            }
        }

        if (mod == null)
        {
            return new ApiResponse<SPTForgeMod> { Success = false, Message = "Cannot find mod by name or guid" };
        }
        
        ForgeMod? forgeMod = LoadFromJson<ForgeMod>(_forgeMods);
        forgeMod = forgeMod ?? new ForgeMod();

        var exists = forgeMod.SptForgeMods.FirstOrDefault(existMod => existMod.Guid.Equals(mod?.Guid));

        ForgeResponse<SPTForgeModVersion>? versionResponse = await FindModDataById(mod?.Id ?? null);
        
        if (exists?.Name != null)
        {
            exists.Name = mod.Name;
            exists.Teaser = mod.Teaser;
            exists.Thumbnail = mod.Thumbnail;
            exists.Detail_Url = mod.Detail_Url;
            exists.Id = mod.Id;
            exists.SptVersions = versionResponse?.Data ?? exists.SptVersions;
        }
        else
        {
            mod.SptVersions = versionResponse?.Data ?? mod.SptVersions;
            forgeMod.SptForgeMods.Add(mod);
        }
        
        SaveToJson(forgeMod, _forgeMods);
        
        return new ApiResponse<SPTForgeMod> { Success = true, Data = mod };
    }
    
    [ApiEndpoint("/smt/api/mods", "GET", Name = "getSptMods",
        Description = "Get all mods from SPT mod list.")]
    public async Task<ApiResponse<SptModResponse>> GetSptMods()
    {
        ServerMod? serverMod = LoadFromJson<ServerMod>(_serverMods);
        ClientMod? clientMod = LoadFromJson<ClientMod>(_activeClientModsPath);
        ForgeMod? forgeMod = LoadFromJson<ForgeMod>(_forgeMods);
        
        var sptModResponse = new SptModResponse();
        sptModResponse.SptServerMods = serverMod?.SptServerMods ?? new List<SPTServerMod>();
        sptModResponse.SptForgeMods = forgeMod?.SptForgeMods ?? new List<SPTForgeMod>();
        sptModResponse.SptClientMods = clientMod?.SptClientMods ?? new Dictionary<string, List<SPTClientMod>>();

        return new ApiResponse<SptModResponse> { Data = sptModResponse };
    }
    
    [ApiEndpoint("/smt/api/mod/active-profile", "POST", Name = "postActiveProfile", Description = "Add all mods if any active profile")]
    public async Task<ApiResponse<object>> PostActiveProfile()
    {
        _profileActivityService.GetActiveProfileIdsWithinMinutes(60)
            .ForEach(profileIdString =>
            {
                var profileId = (MongoId)profileIdString;
                var mods = _profileActivityService.GetProfileActiveClientMods(profileId);
                SaveActiveClientMods(mods, profileId);
            });
        
        return new ApiResponse<object> { Success = true };
    }

    [ApiEndpoint("/smt/api/mod/server", "POST", Name = "postServerMod", Description = "Add all mods on server")]
    public async Task<ApiResponse<object>> PostServerMod()
    {
        
        List<SptMod> loadedMods = [];
        if (ProgramStatics.MODS())
        {
            loadedMods = ModDllLoader.LoadAllMods();
        }

        if (loadedMods.Count == 0)
        {
            _logger.Warning("No active mods found");
            return new ApiResponse<object> { Success = true, Message = "No active mods found"};
        }
        
        ServerMod? serverMod = LoadFromJson<ServerMod>(_serverMods);
        serverMod = serverMod ?? new ServerMod();

        loadedMods.ForEach(mod =>
        {
            var modMeta = mod.ModMetadata;
            var exists = serverMod.SptServerMods
                .Any(existMod => existMod.Guid.Equals(modMeta.ModGuid));

            if (exists) return;
            
            var sptMod = new SPTServerMod();
            sptMod.Guid = modMeta.ModGuid;
            sptMod.SptVersion = modMeta.SptVersion.ToString();
            sptMod.Name = modMeta.Name;
            sptMod.Author = modMeta.Author;
            sptMod.ModVersion = modMeta.Version.ToString();
            
            serverMod.SptServerMods.Add(sptMod);
        });
        
        SaveToJson(serverMod, _serverMods);
        
        return new ApiResponse<object> { Success = true };
    }

    public async Task<ForgeModResponse?> FindModDataByGuid(string modGuid)
    {   
        return await client.GetFromJsonAsync<ForgeModResponse>(
            $"https://forge.sp-tarkov.com/api/v0/mods?filter[guid]={modGuid}");
    }    
    
    public async Task<ForgeResponse<SPTForgeModVersion>?> FindModDataById(int? modId)
    {   
        if(modId == null) return null;
        return await client.GetFromJsonAsync<ForgeResponse<SPTForgeModVersion>>(
            $"https://forge.sp-tarkov.com/api/v0/mod/{modId}/versions") ?? new();
    }
    
    public async Task<ForgeModResponse?> FindModDataByName(string name)
    {   
        return await client.GetFromJsonAsync<ForgeModResponse>(
            $"https://forge.sp-tarkov.com/api/v0/mods?filter[name]={name}");
    }
    
    
    private void SaveActiveClientMods(IReadOnlyList<ProfileActiveClientMods> mods, MongoId sessionID)
    {
        string? pmcName = _profileHelper
            .GetCompleteProfile(sessionID)
            .Where(p => !p.Info.Side.Equals("Savage"))
            .Select(p => p.Info?.Nickname ?? null)
            .FirstOrDefault();

        pmcName = pmcName ?? sessionID.ToString();
    
        ClientMod? clientMods = LoadFromJson<ClientMod>(_activeClientModsPath);
        clientMods = clientMods ?? new ClientMod();

        if (!clientMods.SptClientMods.ContainsKey(pmcName))
        {
            clientMods.SptClientMods.Add(pmcName, new List<SPTClientMod>());
        }
        
        mods.Select(cm =>
            {
                var _clientMod = new SPTClientMod();
                _clientMod.Name = cm.Name;
                _clientMod.Guid = cm.GUID;
                _clientMod.ModVersion = cm.Version.ToString();

                return _clientMod;
            }).ToList().ForEach(mod =>
      {
          var exists = clientMods.SptClientMods[pmcName]
              .Any(existMod => existMod.Guid.Equals(mod.Guid));

          if (exists) return;
              
          clientMods.SptClientMods[pmcName].Add(mod);
      });
        
        SaveToJson(clientMods, _activeClientModsPath);
    }

    public T? LoadFromJson<T>(string path)
    {
        try
        {
            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            var fileInfo = new FileInfo(dataPath);

            if (fileInfo.Directory == null || !fileInfo.Directory.Exists)
            {
                return default;
            }

            var jsonContent = File.ReadAllText(dataPath);
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch (Exception ex)
        {
            //
        }
        return default;
    }

    private void SaveToJson(Object data, string path)
    {
        try
        {
            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            var fileInfo = new FileInfo(dataPath);

            fileInfo.Directory?.Create();

            var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dataPath, jsonString);
        }
        catch (Exception ex)
        {
            _logger.Error($"[SMT] on save data", ex);
        }
    }
}
