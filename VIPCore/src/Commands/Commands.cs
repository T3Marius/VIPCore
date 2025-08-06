using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using System.Text;
using VipCoreApi;
using static VIPCore.VIPCore;

namespace VIPCore;

public static class Commands
{
    public static void Register()
    {
        var CmdConfig = Instance.Config.Commands;
        var AddCmd = Instance.AddCommand;

        foreach (var cmd in CmdConfig.VipMenu)
        {
            AddCmd($"css_{cmd}", "Opens the VIP Menu", (p, info) => VIPMenu.Display(p!));
        }
        foreach (var cmd in CmdConfig.AddVip)
        {
            AddCmd($"css_{cmd}", "Adds a vip", Command_AddVip);
        }
        foreach (var cmd in CmdConfig.UpdateVip)
        {
            AddCmd($"css_{cmd}", "Updates a vip", Command_UpdateVIp);
        }
        foreach (var cmd in CmdConfig.ListVips)
        {
            AddCmd($"css_{cmd}", "Lists all vips", Command_VipList);
        }
        foreach (var cmd in CmdConfig.BuyVip)
        {
            AddCmd($"css_{cmd}", "Sends a link to buy vip", Command_BuyVip);
        }
        foreach (var cmd in CmdConfig.FreeVip)
        {
            AddCmd($"css_{cmd}", "Gives free vip", Command_FreeVip);
        }
        foreach (var cmd in CmdConfig.RemoveVip)
        {
            AddCmd($"css_{cmd}", "Removes a vip", Command_RemoveVip);
        }
    }
    private static void Command_RemoveVip(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;
        if (!ulong.TryParse(info.GetArg(1), out ulong steamId))
        {
            player.SendChatLocalizedMessage("vip.InvalidSteamID");
            return;
        }

        string playerName = Lib.GetPlayerNameFromSteamID(steamId).GetAwaiter().GetResult();

        if (!VIPManager.IsPlayerVip(steamId))
        {
            player.SendChatLocalizedMessage("vip.PlayerNotVip", playerName);
            return;
        }
        VIPManager.RemovePlayerVip(steamId);
        player.SendChatLocalizedMessage("vip.VipRemoved", playerName);
    }
    private static void Command_FreeVip(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (VIPManager.IsPlayerVip(player.SteamID))
        {
            player.SendChatLocalizedMessage("vip.AlreadyVip");
            return;
        }


        var allFreeVipGroups = new Dictionary<string, (string TranslationKey, int DurationMinutes)>
        {
            { "SILVER-VIP", ("vip.FreeVipSilver", 20) },
            { "GOLD-VIP", ("vip.FreeVipGold", 15) },
            { "PLATINUM-VIP", ("vip.FreeVipPlatinum", 10) }
        };

        var usedGroups = Database.GetUsedFreeVipGroups(player.SteamID);

        var availableGroups = allFreeVipGroups
            .Where(kvp => !usedGroups.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (availableGroups.Count == 0)
        {
            player.SendChatLocalizedMessage("vip.NoFreeVipGroups");
            return;
        }

        var menuTitle = LangApi.GetPlayerTranslation(player, Instance.ModuleName, "vip.FreeVipTitle");
        IT3Menu menu = MenuManager.CreateMenu(menuTitle);

        foreach (var group in availableGroups)
        {
            string groupName = group.Key;
            string translationKey = group.Value.TranslationKey;
            int duration = group.Value.DurationMinutes;

            string menuText = LangApi.GetPlayerTranslation(player, Instance.ModuleName, translationKey);

            menu.AddOption(menuText, (p, option) =>
            {
                if (!p.IsValid) return;

                bool success = Database.GrantFreeVip(p.SteamID, groupName, TimeSpan.FromMinutes(duration));

                if (success)
                {
                    string friendlyGroupName = menuText.Split('(')[0].Trim();
                    p.SendChatLocalizedMessage("vip.FreeVipGranted", friendlyGroupName, duration);
                }

                MenuManager.CloseMenu(p);
            });
        }

        MenuManager.OpenMainMenu(player, menu);
    }
    private static void Command_BuyVip(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        player.SendChatLocalizedMessage("vip.BuyVip");
    }
    private static void Command_VipList(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        var menu = MenuManager.CreateMenu(LangApi.GetPlayerTranslation(player, Instance.ModuleName, "vip.ListTitle"));
        menu.HasSound = false;

        StringBuilder stb = new StringBuilder();
        int count = 1;
        foreach (var vip in Utilities.GetPlayers().Where(p => VIPManager.IsPlayerVip(p.SteamID) && p.Connected == PlayerConnectedState.PlayerConnected))
        {
            VipInfo? vipInfo = VIPManager.GetPlayerVipInfo(vip.SteamID);
            if (vipInfo == null)
                continue;

            string message = $"#{count} - {vip.PlayerName} | {vipInfo.Group}";

            menu.AddTextOption(message, true);
            count++;
        }

        MenuManager.OpenMainMenu(player, menu);

    }
    [CommandHelper(minArgs: 3, usage: "<steamid> <group> <duration> in minutes")]
    private static void Command_AddVip(CCSPlayerController? player, CommandInfo info)
    {
        if (!ulong.TryParse(info.GetArg(1), out ulong steamId))
        {
            if (player != null)
            {
                player.SendChatLocalizedMessage("vip.InvalidSteamID");
            }
            else
            {
                Instance.Logger.LogInformation("The SteamID you eneter is invalid.");
            }
            return;
        }

        string group = info.GetArg(2);
        if (!Instance.Config.Groups.Keys.Any(gr => gr == group))
        {
            if (player != null)
            {
                player.SendChatLocalizedMessage("vip.InvalidGroup", group);
            }
            else
            {
                Instance.Logger.LogInformation($"The group '{group}' is not a valid VIP group.");
            }
            return;
        }

        TimeSpan duration;
        if (!int.TryParse(info.GetArg(3), out int minutes))
        {
            if (player != null)
            {
                player.SendChatLocalizedMessage("vip.InvalidDuration");
            }
            else
            {
                Instance.Logger.LogInformation("The duration you entered is invalid.");
            }
            return;
        }
        duration = TimeSpan.FromMinutes(minutes);

        if (VIPManager.IsPlayerVip(steamId))
        {
            if (player != null)
            {
                player.SendChatLocalizedMessage("vip.PlayerAlreadyVip", steamId);
            }
            else
            {
                Instance.Logger.LogInformation($"The player with SteamID {steamId} is already a VIP.");
            }
            return;
        }

        VIPManager.AddPlayerVip(steamId, group, duration);

        string playerName = Lib.GetPlayerNameFromSteamID(steamId).GetAwaiter().GetResult();

        if (player != null)
        {
            Server.NextFrame(() => player.SendChatLocalizedMessage("vip.VipAdded", group, playerName, minutes));
        }
        else
        {
            Instance.Logger.LogInformation($"Added VIP for {playerName} in group '{group}' for {minutes} minutes.");
        }
    }
    [CommandHelper(minArgs: 3, usage: "<steamid> <newgroup> <newduration> in minutes")]
    private static void Command_UpdateVIp(CCSPlayerController? player, CommandInfo info)
    {
        if (!ulong.TryParse(info.GetArg(1), out ulong steamId))
        {
            if (player != null)
            {
                player.SendChatLocalizedMessage("vip.InvalidSteamID");
            }
            else
            {
                Instance.Logger.LogInformation("The SteamID you eneter is invalid.");
            }
            return;
        }

        string group = info.GetArg(2);
        if (!Instance.Config.Groups.Keys.Any(gr => gr == group))
        {
            if (player != null)
            {
                player.SendChatLocalizedMessage("vip.InvalidGroup", group);
            }
            else
            {
                Instance.Logger.LogInformation($"The group '{group}' is not a valid VIP group.");
            }
            return;
        }

        TimeSpan duration;
        if (!int.TryParse(info.GetArg(3), out int minutes))
        {
            if (player != null)
            {
                player.SendChatLocalizedMessage("vip.InvalidDuration");
            }
            else
            {
                Instance.Logger.LogInformation("The duration you entered is invalid.");
            }
            return;
        }
        duration = TimeSpan.FromMinutes(minutes);

        VIPManager.UpdatePlayerVip(steamId, group, duration);

        string playerName = Lib.GetPlayerNameFromSteamID(steamId).GetAwaiter().GetResult();

        if (player != null)
        {
            Server.NextFrame(() => player.SendChatLocalizedMessage("vip.UpdatedVip", group, playerName, minutes));
        }
        else
        {
            Instance.Logger.LogInformation($"Updated VIP for {playerName} to group '{group}' for {minutes} minutes.");
        }
    }
}
