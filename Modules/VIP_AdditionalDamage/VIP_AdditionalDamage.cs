using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VipCoreApi;

namespace VIPCore;

public class VIPAdditionalDamage : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] Additional Damage";
    public override string ModuleVersion => "1.0.0";
    public static IVipCoreApi VipApi = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VipApi = IVipCoreApi.Capability.Get() ?? throw new Exception("Failed to get IVipCoreApi capability");

        VipApi.RegisterFeature(new VIP_AdditionalDamage(this));
    }
}
public class VIP_AdditionalDamage : IVipFeatureBase
{
    private readonly BasePlugin _plugin = null!;
    private readonly IVipCoreApi VipApi = VIPAdditionalDamage.VipApi;
    public string FeatureName => "Additional Damage";
    public void OnFeatureLoaded()
    {

    }
    public VIP_AdditionalDamage(BasePlugin plugin)
    {
        _plugin = plugin;
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamageOldFunc, HookMode.Pre);
    }
    private HookResult OnTakeDamageOldFunc(DynamicHook hook)
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
            var dmgBonus = VipApi.GetFeatureValue<int>(controller.SteamID, FeatureName);

            if (dmgBonus.HasValue && dmgBonus.Value > 0)
            {
                info.Damage += dmgBonus.Value;
                return HookResult.Changed;
            }
        }


        return HookResult.Continue;
    }
}