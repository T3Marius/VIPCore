using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text.Json;
using VipCoreApi;

namespace VIPCore;

public class VIPArmor :BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] Armor";
    public override string ModuleVersion => "1.0.0";

    public static IVipCoreApi VipApi = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VipApi = IVipCoreApi.Capability.Get() ?? throw new Exception("VipCoreApi not found");

        VipApi.RegisterFeature(new VIP_Armor(this));
    }
}

public class VIP_Armor : IVipFeatureBase
{
    private readonly BasePlugin _plugin = null!;
    public string FeatureName => "Armor";
    private IVipCoreApi VipApi => VIPArmor.VipApi;
    public void OnFeatureLoaded()
    {

    }
    public VIP_Armor(BasePlugin plugin)
    {
        _plugin = plugin;

        _plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid || VipApi == null)
            return HookResult.Continue;

        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if (VipApi.IsPlayerVip(player.SteamID) && VipApi.IsPlayerFeatureEnabled(player.SteamID, FeatureName))
        {

            var armorBonus = VipApi.GetFeatureValue<int>(player.SteamID, FeatureName);

            if (armorBonus.HasValue && armorBonus.Value > 0)
            {
                pawn.ArmorValue = armorBonus.Value;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
            }
        }

        return HookResult.Continue;
    }
}
