using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using LanguageApi;
using Microsoft.Extensions.Logging;
using T3MenuSharedApi;
using VipCoreApi;

namespace VIPCore;

public class VIPCore : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] Core";
    public override string ModuleVersion => "1.0.0";

    public static VIPCore Instance { get; set; } = new();
    public static VipApi VipApi { get; set; } = new();
    public static ILanguageApi LangApi = null!;
    public PluginConfig Config { get; set; } = new();
    public static IT3MenuManager MenuManager { get; set; } = null!;
    public IT3MenuManager GetMenuManager()
    {
        if (MenuManager == null)
        {
            MenuManager = new PluginCapability<IT3MenuManager>("t3menu:manager").Get() ?? throw new Exception("T3MenuManager not found");
        }
        return MenuManager;
    }

    public override void Load(bool hotReload)
    {
        Instance = this;
        ConfigureApi();

        Task.Run(async () =>
        {
            await Database.InitializeAsync(Config.Database);
        });

        Events.Initialize();
        Commands.Register();
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VIPManager.Initialize(Config, VipApi);
        LangApi = ILanguageApi.Capability.Get() ?? throw new Exception("LanguageApi not found");
        GetMenuManager();

        LangApi.LoadPluginTranslations(ModuleName, Path.Combine(ModuleDirectory, "Translations", "Translations.json"));

        AddTimer(5.0f, () =>
        {
            Logger.LogInformation("VIP Core fully initialized with {0} VIP groups", VIPManager.GetAvailableGroups().Count());
        });
    }

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;

        foreach (var group in Config.Groups)
        {
            if (group.Value.Features.Count == 0)
            {
                Logger.LogWarning($"VIP Group '{0}' has no features configured", group.Key);
            }
        }
    }

    private void ConfigureApi()
    {
        string configPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", "VIPCore", "VIPCore.json");

        VipApi.SetConfigPath(configPath);


        Capabilities.RegisterPluginCapability(IVipCoreApi.Capability, () => VipApi);
    }
}