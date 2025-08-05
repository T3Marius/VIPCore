using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Text.Json;
using VipCoreApi;

namespace VIPCore;

public class VIPHealth : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] Health";
    public override string ModuleVersion => "1.0.0";
    public static IVipCoreApi VipApi = null!;
    
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VipApi = IVipCoreApi.Capability.Get() ?? throw new Exception("VipApi not found");
        VipApi.RegisterFeature(new VIP_Health_Feature(this));
    }
}

public class VIP_Health_Feature : IVipFeatureBase
{
    private readonly BasePlugin _plugin;
    public string FeatureName => "Health";
    private IVipCoreApi VipApi => VIPHealth.VipApi;

    public void OnFeatureLoaded()
    {

    }

    public VIP_Health_Feature(BasePlugin plugin)
    {
        _plugin = plugin;
        _plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? spawningPlayer = @event.Userid;

        if (spawningPlayer == null || !spawningPlayer.IsValid || spawningPlayer.IsBot)
        {
            return HookResult.Continue;
        }

        Server.NextFrame(() =>
        {
            if (!spawningPlayer.IsValid || spawningPlayer.Pawn.Value == null)
            {
                return;
            }

            if (!VipApi.IsPlayerVip(spawningPlayer.SteamID) || !VipApi.IsPlayerFeatureEnabled(spawningPlayer.SteamID, FeatureName))
            {
                return;
            }

            var healthBonus = VipApi.GetFeatureValue<int>(spawningPlayer.SteamID, FeatureName);

            if (healthBonus.HasValue && healthBonus.Value > 0)
            {
                CCSPlayerPawn? pawn = spawningPlayer.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid)
                {
                    pawn.Health = healthBonus.Value;
                    pawn.MaxHealth = healthBonus.Value;

                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                }
            }
        });

        return HookResult.Continue;
    }
}