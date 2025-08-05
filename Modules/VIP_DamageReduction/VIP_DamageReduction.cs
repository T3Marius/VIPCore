
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Text.Json;
using VipCoreApi;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace VIPCore;

public class VIPDamageReduction : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] DamageReduction";
    public override string ModuleVersion => "1.0.0";
    public static IVipCoreApi VipApi = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VipApi = IVipCoreApi.Capability.Get() ?? throw new Exception("VipApi not found");
        VipApi.RegisterFeature(new VIP_DamageReduction(this));
    }
}

public class VIP_DamageReduction : IVipFeatureBase
{
    private readonly BasePlugin _plugin;
    public string FeatureName => "Damage Reduction";
    private IVipCoreApi VipApi => VIPDamageReduction.VipApi;

    public void OnFeatureLoaded()
    {
        // Feature loaded logic here
    }

    public VIP_DamageReduction(BasePlugin plugin)
    {
        _plugin = plugin;
        _plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
    }
    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;

        if (victim == null || victim == @event.Attacker || victim.PlayerPawn.Value == null)
            return HookResult.Continue;

        if (!VipApi.IsPlayerVip(victim.SteamID) && !VipApi.IsPlayerFeatureEnabled(victim.SteamID, FeatureName))
            return HookResult.Continue;

        var dmgReduction = VipApi.GetFeatureValue<int>(victim.SteamID, FeatureName);
        if (dmgReduction == null)
            return HookResult.Continue;

        if (dmgReduction.HasValue && dmgReduction.Value > 0)
        {
            victim.PlayerPawn.Value.Health += dmgReduction.Value;
            Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
            return HookResult.Changed;
        }

        return HookResult.Continue;
    }
}
