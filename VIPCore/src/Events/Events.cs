using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static VIPCore.VIPCore;

namespace VIPCore;

public static class Events
{
    public static void Initialize()
    {
        Instance.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }
    private static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Task.Run(async () =>
        {
            await Database.RemoveExpiredVipsAsync();
            await Database.RemoveExpiredFreeVipsAsync();
        });

        return HookResult.Continue;
    }
    private static HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        Task.Run(async () =>
        {
            await Database.RemoveExpiredVipsAsync();
            await Database.RemoveExpiredFreeVipsAsync();
        });

        if (VIPCore.VipApi.IsPlayerVip(player.SteamID))
        {
            var vipInfo = VIPCore.VipApi.GetPlayerVipInfo(player.SteamID);
            if (vipInfo == null)
                return HookResult.Continue;

            DateTimeOffset expiryDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(vipInfo.ExpiryTimestamp);

            DateTime expiryLocalDateTime = expiryDateTimeOffset.LocalDateTime;

            string formattedExpiryDate = expiryLocalDateTime.ToString("yyyy/MM/dd HH:mm");


            Server.NextFrame(() =>
            {
                player.SendChatLocalizedMessage("vip.OnConnect", player.PlayerName, vipInfo.Group, formattedExpiryDate);
            });
        }

        return HookResult.Continue;
    }
}
