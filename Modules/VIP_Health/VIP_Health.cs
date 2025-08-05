using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
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
        VipApi.RegisterFeature(new VIP_Health(this));
    }
}

public class VIP_Health : IVipFeatureBase
{
    private readonly BasePlugin _plugin;
    public string FeatureName => "Health";
    private IVipCoreApi VipApi => VIPHealth.VipApi;

    public void OnFeatureLoaded() { }

    public VIP_Health(BasePlugin plugin)
    {
        _plugin = plugin;
        _plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }


    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || player.PlayerPawn.Value == null)
            return HookResult.Continue;

        if (!VipApi.IsPlayerVip(player.SteamID) && !VipApi.IsPlayerFeatureEnabled(player.SteamID, FeatureName))
            return HookResult.Continue;

        var health = VipApi.GetFeatureValue<int>(player.SteamID, FeatureName)!.Value;
        if (health > 0)
        {
            Server.NextFrame(() =>
            {
                player.PlayerPawn.Value.Health = health;
                player.PlayerPawn.Value.MaxHealth = player.PlayerPawn.Value.Health;

                Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(player, "CBaseEntity", "m_iMaxHealth");
            });
        }

        return HookResult.Continue;
    }
}