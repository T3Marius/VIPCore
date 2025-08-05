
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Text.Json;
using VipCoreApi;

namespace VIPCore;

public class VIPHealthKill : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] HealthKill";
    public override string ModuleVersion => "1.0.0";
    public static IVipCoreApi VipApi = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VipApi = IVipCoreApi.Capability.Get() ?? throw new Exception("VipApi not found");
        VipApi.RegisterFeature(new VIP_HealthKill(this));
    }
}

public class VIP_HealthKill : IVipFeatureBase
{
    private readonly BasePlugin _plugin;
    public string FeatureName => "HealthKill";
    private IVipCoreApi VipApi => VIPHealthKill.VipApi;

    public void OnFeatureLoaded()
    {
    }

    public VIP_HealthKill(BasePlugin plugin)
    {
        _plugin = plugin;
        _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Post);
    }
    private HookResult OnTakeDamage(DynamicHook hook)
    {
        var victim = hook.GetParam<CBaseEntity>(0);
        if (victim.DesignerName != "player")
            return HookResult.Continue;

        var info = hook.GetParam<CTakeDamageInfo>(1);
        var attackerHandle = info.Attacker;
        if (attackerHandle.Value == null || !attackerHandle.IsValid || attackerHandle.Value.DesignerName != "player")
            return HookResult.Continue;

        var attacker = attackerHandle.Value.As<CCSPlayerPawn>();
        var controller = attacker.OriginalController.Value;
        if (controller == null || !controller.IsValid)
            return HookResult.Continue;

        if (VipApi.IsPlayerVip(controller.SteamID) && VipApi.IsPlayerFeatureEnabled(controller.SteamID, FeatureName))
        {
            var healthKillConfig = VipApi.GetFeatureConfig<HealthKillConfig>(controller.SteamID, FeatureName);
            if (healthKillConfig == null)
                return HookResult.Continue;

            if (healthKillConfig.BulletHit > 0)
            {
                SetHealth(controller, controller.PlayerPawn.Value!.Health + healthKillConfig.BulletHit);
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }
        return HookResult.Continue;
    }
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;
        CCSPlayerController? assister = @event.Assister;

        if (victim == null || attacker == null || attacker == victim)
            return HookResult.Continue;

        if (attacker.PlayerPawn.Value == null)
            return HookResult.Continue;

        if (VipApi.IsPlayerVip(attacker.SteamID) && VipApi.IsPlayerFeatureEnabled(attacker.SteamID, FeatureName))
        {
            var healthKillConfig = VipApi.GetFeatureConfig<HealthKillConfig>(attacker.SteamID, FeatureName);
            if (healthKillConfig == null)
                return HookResult.Continue;

            if (healthKillConfig.Kill > 0)
            {
                SetHealth(attacker, attacker.PlayerPawn.Value.Health + healthKillConfig.Kill);
                return HookResult.Changed;
            }
        }
        if (assister != null && assister.PlayerPawn.Value != null && VipApi.IsPlayerVip(assister.SteamID) && VipApi.IsPlayerFeatureEnabled(assister.SteamID, FeatureName))
        {
            var healthKillConfig = VipApi.GetFeatureConfig<HealthKillConfig>(assister.SteamID, FeatureName);
            if (healthKillConfig == null)
                return HookResult.Continue;

            if (healthKillConfig.Assist > 0)
            {
                SetHealth(assister, assister.PlayerPawn.Value.Health + healthKillConfig.Assist);
                return HookResult.Changed;
            }
        }

        return HookResult.Continue;
    }
    private void SetHealth(CCSPlayerController player, int health)
    {
        if (player.PlayerPawn.Value == null)
            return;

        CCSPlayerPawn? pawn = player.PlayerPawn.Value;

        pawn.Health = health;
        pawn.MaxHealth = health;

        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    }
}
public class HealthKillConfig
{
    public int Kill { get; set; } = 3;
    public int Assist { get; set; } = 3;
    public int BulletHit { get; set; } = 2;

}


