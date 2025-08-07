using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Text.Json;
using VipCoreApi;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Logging;

namespace VIPCore;

public class DecoyTeleport : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[VIP] DecoyTeleport";
    public override string ModuleVersion => "1.0.0";
    public static IVipCoreApi VipApi = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        VipApi = IVipCoreApi.Capability.Get() ?? throw new Exception("VipApi not found");
        VipApi.RegisterFeature(new VIP_DecoyTeleport(this));
    }
}

public class VIP_DecoyTeleport : IVipFeatureBase
{
    private readonly BasePlugin _plugin;
    public string FeatureName => "DecoyTeleport";
    private IVipCoreApi VipApi => DecoyTeleport.VipApi;
    private Dictionary<int, CDecoyProjectile> _decoyTeleports = new();

    public void OnFeatureLoaded()
    {
        // Feature loaded logic here

    }

    public VIP_DecoyTeleport(BasePlugin plugin)
    {
        _plugin = plugin;

        _plugin.RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
        _plugin.RegisterEventHandler<EventDecoyStarted>(OnDecoyFiring);
        _plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        foreach (var player in Utilities.GetPlayers().Where(p => VipApi.IsPlayerVip(p.SteamID) && VipApi.IsPlayerFeatureEnabled(p.SteamID, FeatureName)))
        {
            Server.NextFrame(() =>
            {
                if (_decoyTeleports.ContainsKey(player.Slot))
                    _decoyTeleports.Remove(player.Slot);

                player.GiveNamedItem(CsItem.Decoy);
            });
        }

        return HookResult.Continue;
    }
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        if (_decoyTeleports.ContainsKey(player.Slot))
            _decoyTeleports.Remove(player.Slot);

        return HookResult.Continue;
    }
    private HookResult OnDecoyFiring(EventDecoyStarted @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        if (_decoyTeleports.TryGetValue(player.Slot, out var decoy))
        {
            _decoyTeleports.Remove(player.Slot);

            Vector? detonatePos = new Vector(@event.X, @event.Y, @event.Z);

            Server.NextFrame(() =>
            {
                if (detonatePos != null)
                {
                    player.PlayerPawn.Value?.Teleport(detonatePos, new QAngle(), new Vector());
                }
            });
        }

        return HookResult.Continue;
    }
    private void OnEntityCreated(CEntityInstance entity)
    {
        Server.NextFrame(() =>
        {
            if (entity.DesignerName != "decoy_projectile")
                return;

            var decoy = entity.As<CDecoyProjectile>();

            if (decoy == null)
            {
                _plugin.Logger.LogInformation("Decoy is null.");
                return;
            }

            CCSPlayerPawn? pawn = decoy.OriginalThrower.Value;

            if (pawn == null)
            {
                _plugin.Logger.LogInformation("pawn is null");
                return;
            }

            CCSPlayerController? player = pawn.OriginalController.Value;
            if (player == null || !VipApi.IsPlayerVip(player.SteamID) || !VipApi.IsPlayerFeatureEnabled(player.SteamID, FeatureName))
            {
                _plugin.Logger.LogInformation("Player is null");
                return;
            }

            if (!_decoyTeleports.ContainsKey(player.Slot))
            {
                _decoyTeleports.Add(player.Slot, decoy);
            }
        });

    }
}