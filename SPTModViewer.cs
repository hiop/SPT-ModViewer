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

[Injectable(TypePriority = 0, InjectionType = InjectionType.Singleton)]
public class SPTModViewer : WebUiModBase
{
    private static readonly string _dataPath = @"user\mods\SPTModViewer\Data\";
    private static readonly string _configPath = @"user\mods\SPTModViewer\Config\SptModViewerConfig.json5";
    private static readonly string _activeClientModsPath = _dataPath + "ActiveClientMods.json5";
    private static readonly string _serverMods = _dataPath + "ServerMods.json5";
    private static readonly string _forgeMods = _dataPath + "ForgeMods.json5";
    
    private static readonly HttpClient client = new HttpClient();
    private static ProfileHelper _profileHelper;
    private static ProfileActivityService _profileActivityService;
    private ISptLogger<SPTModViewer> logger;
    protected override string BasePath => "/smv";

    public SPTModViewer(ModHelper modHelper, ISptLogger<SPTModViewer> logger, ProfileActivityService profileActivityService, ProfileHelper profileHelper)
        : base(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()))
    {
        this.logger = logger;
        _profileHelper = profileHelper;
        _profileActivityService = profileActivityService;

        var config = LoadFromJson<SPTModViewerConfig>(_configPath);

        if (config == null || config?.ForgeApiToken == null)
        {
            logger.Error("[SPT Mod Viewer] config file not exist or Forge API token not found");
        }
        
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config?.ForgeApiToken);
    }

    [ApiEndpoint("/smv/api/forge-mod", "POST", Name="updateModByForge", Description = "Update mod with forge if possible")]
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

        int page = 1;
        
        ForgeResponse<SPTForgeModVersion>? versionResponse = await FindModVersionById(mod?.Id ?? null, page);

        while (versionResponse?.Meta.LastPage != page)
        {
            page++;
            var _versionResposne =  await FindModVersionById(mod?.Id ?? null, page);
            versionResponse?.Data.AddRange(_versionResposne?.Data ?? []);
            await Task.Delay(1000);
        }
        
        if (exists?.Name != null)
        {
            exists.Name = mod.Name;
            exists.Teaser = mod.Teaser;
            exists.Thumbnail = mod.Thumbnail;
            exists.DetailUrl = mod.DetailUrl;
            exists.Id = mod.Id;
            exists.SptVersions = versionResponse?.Data ?? exists.SptVersions;
        }
        else
        {
            mod.SptVersions = versionResponse?.Data ?? mod.SptVersions;
            forgeMod.SptForgeMods.Add(mod);
        }
        
        SaveToJson(forgeMod, _forgeMods);
        
        return new ApiResponse<SPTForgeMod> { Success = true, Data = exists ?? mod };
    }
    
    [ApiEndpoint("/smv/api/mods", "GET", Name = "getSptMods",
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
    
    [ApiEndpoint("/smv/api/mod/active-profile", "POST", Name = "postActiveProfile", Description = "Add all mods if any active profile")]
    public async Task<ApiResponse<object>> PostActiveProfile()
    {
        try
        {
            _profileActivityService.GetActiveProfileIdsWithinMinutes(60)
                .ForEach(profileIdString =>
                {
                    var profileId = (MongoId)profileIdString;
                    var mods = _profileActivityService.GetProfileActiveClientMods(profileId);
                    SaveActiveClientMods(mods, profileId);
                });
        }
        catch (Exception e)
        {
            if (e.StackTrace != null) logger.Warning(e.StackTrace);
            return new ApiResponse<object> { Success = false, Message = e.Message };
        }

        return new ApiResponse<object> { Success = true };
    }

    [ApiEndpoint("/smv/api/mod/server", "POST", Name = "postServerMod", Description = "Add all mods on server")]
    public async Task<ApiResponse<object>> PostServerMod()
    {
        try
        {
            List<SptMod> loadedMods = [];
            if (ProgramStatics.MODS())
            {
                loadedMods = ModDllLoader.LoadAllMods();
            }

            if (loadedMods.Count == 0)
            {
                logger.Warning("No active mods found");
                return new ApiResponse<object> { Success = true, Message = "No active mods found" };
            }

            ServerMod? serverMod = LoadFromJson<ServerMod>(_serverMods);
            serverMod = serverMod ?? new ServerMod();

            loadedMods.ForEach(mod =>
            {
                var modMeta = mod.ModMetadata;
                var exists = serverMod.SptServerMods
                    .FirstOrDefault(existMod => existMod.Guid.Equals(modMeta.ModGuid));

                if (exists?.Guid != null)
                {
                    exists.Guid = modMeta.ModGuid;
                    exists.SptVersion = modMeta.SptVersion.ToString();
                    exists.Name = modMeta.Name;
                    exists.Author = modMeta.Author;
                    exists.ModVersion = modMeta.Version.ToString();
                }
                else
                {
                    var sptMod = new SPTServerMod();
                    sptMod.Guid = modMeta.ModGuid;
                    sptMod.SptVersion = modMeta.SptVersion.ToString();
                    sptMod.Name = modMeta.Name;
                    sptMod.Author = modMeta.Author;
                    sptMod.ModVersion = modMeta.Version.ToString();

                    serverMod.SptServerMods.Add(sptMod);
                }

            });

            SaveToJson(serverMod, _serverMods);
        }
        catch (Exception e)
        {
            if (e.StackTrace != null) logger.Warning(e.StackTrace);
            return new ApiResponse<object> { Success = false, Message = e.Message };
        }

        return new ApiResponse<object> { Success = true };
    }

    [ApiEndpoint("/smv/api/mod/profile/hide", "POST", Name = "hideProfileMod", Description = "Hide profile mod")]
    public async Task<ApiResponse<object>> HideClientMod(HideClientMod hideMod)
    {
        ClientMod? clientMod = LoadFromJson<ClientMod>(_activeClientModsPath);
        
        var existMode = clientMod.SptClientMods[hideMod.ClientName]
            .FirstOrDefault(m => m.Guid.Equals(hideMod.Guid));

        if (existMode?.Guid != null)
        {
            existMode.Visible = false;
        }
        
        SaveToJson(clientMod, _activeClientModsPath);
        
        return new ApiResponse<object> { Success = true };
    }
    
    [ApiEndpoint("/smv/api/mod/server/hide", "POST", Name = "hideServerMod", Description = "Hide server mod")]
    public async Task<ApiResponse<object>> HideServerMod(HideServerMod hideMod)
    {
        ServerMod? serverMods = LoadFromJson<ServerMod>(_serverMods);
        if (serverMods == null)
        {
            return new ApiResponse<object> { Success = false, Message = "No active server mods found" };
        } 
        
        var existMode = serverMods?.SptServerMods
            .FirstOrDefault(m => m.Guid.Equals(hideMod.Guid));

        if (existMode?.Guid != null)
        {
            existMode.Visible = false;
        }
        
        SaveToJson(serverMods, _serverMods);
        
        return new ApiResponse<object> { Success = true };
    }

    public async Task<ForgeModResponse?> FindModDataByGuid(string modGuid)
    {   
        return await client.GetFromJsonAsync<ForgeModResponse>(
            $"https://forge.sp-tarkov.com/api/v0/mods?filter[guid]={modGuid}");
    }    
    
    public async Task<ForgeResponse<SPTForgeModVersion>?> FindModVersionById(int? modId, int page)
    {   
        if(modId == null) return null;
        
        return await client.GetFromJsonAsync<ForgeResponse<SPTForgeModVersion>>(
            $"https://forge.sp-tarkov.com/api/v0/mod/{modId}/versions?page={page}") ?? new();
        
    }
    
    public async Task<ForgeModResponse?> FindModDataByName(string name)
    {   
        return await client.GetFromJsonAsync<ForgeModResponse>(
            $"https://forge.sp-tarkov.com/api/v0/mods?filter[name]={name}");
    }
    
    
    private void SaveActiveClientMods(IReadOnlyList<ProfileActiveClientMods> mods, MongoId sessionID)
    {
        string? pmcName = sessionID.ToString();
        try
        {
            pmcName = _profileHelper
                .GetCompleteProfile(sessionID)
                .Where(p => !p.Info.Side.Equals("Savage"))
                .Select(p => p.Info?.Nickname ?? null)
                .FirstOrDefault();
        }
        catch
        {
            //
        }
        
        pmcName = pmcName ?? sessionID.ToString();
        if(pmcName.Length <= 0) return;
    
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
              .FirstOrDefault(existMod => existMod.Guid.Equals(mod.Guid));

          if (exists?.Guid != null)
          {
              exists.Guid = mod.Guid;
              exists.ModVersion = mod.ModVersion;
              exists.Name = mod.Name;
          }
          else
          {
              clientMods.SptClientMods[pmcName].Add(mod);
          }
 
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
            logger.Error($"[SMT] on save data", ex);
        }
    }
}
