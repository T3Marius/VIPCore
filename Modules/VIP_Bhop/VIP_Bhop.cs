using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using FurienAPI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VipCoreApi;

namespace VIPCore;

public class VIPBhop : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] Bhop";
    public override string ModuleVersion => "1.0.0";

    public static IFurienApi FurienApi = null!;
    public static IVipCoreApi VipApi = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VipApi = IVipCoreApi.Capability.Get() ?? throw new Exception("VipApi not found");
        FurienApi = IFurienApi.Capability.Get() ?? throw new Exception("FurienApi not found");

        VipApi.RegisterFeature(new VIP_Bhop(this));
    }
}
public class VIP_Bhop : IVipFeatureBase
{
    private readonly BasePlugin _plugin = null!;
    public string FeatureName => "Bhop";
    public IVipCoreApi VipApi => VIPBhop.VipApi;
    public IFurienApi FurienApi => VIPBhop.FurienApi;
    public void OnFeatureLoaded()
    {

    }
    public VIP_Bhop(BasePlugin plugin)
    {
        _plugin = plugin;
        _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid)
                continue;

            if (VipApi.IsPlayerVip(player.SteamID) && VipApi.IsPlayerFeatureEnabled(player.SteamID, FeatureName))
            {
                var bhopConfig = VipApi.GetFeatureConfig<BhopConfig>(player.SteamID, FeatureName);
                if (bhopConfig == null)
                    return HookResult.Continue;

                if (bhopConfig.MaxSpeed > 0)
                {
                    FurienApi.GiveBhop(player, bhopConfig.MaxSpeed, 2);
                }
                else
                {
                    FurienApi.RemoveBhop(player);
                }
            }
        }
        return HookResult.Continue;
    }
}
public class BhopConfig
{
    public float MaxSpeed { get; set; } = 300f;
}
