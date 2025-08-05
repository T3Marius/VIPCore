using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using static VIPCore.VIPCore;
using VipCoreApi;

namespace VIPCore;

public static class VIPMenu
{
    public static void Display(CCSPlayerController player)
    {
        var MenuManager = Instance.GetMenuManager();
        if (MenuManager == null)
        {
            Instance.Logger.LogError("MenuManager is null. use GetMenuManager() in command.");
            return;
        }

        if (!VIPManager.IsPlayerVip(player.SteamID))
        {
            player.SendChatLocalizedMessage("not.Vip");
            return;
        }

        var vipInfo = VIPCore.VipApi.GetPlayerVipInfo(player.SteamID);
        if (vipInfo == null)
            return;

        IT3Menu vipMenu = MenuManager.CreateMenu(LangApi.GetPlayerTranslation(player, Instance.ModuleName, "vip.Title", vipInfo.Group));

        vipMenu.HasSound = false;

        foreach (var kvp in Instance.Config.Groups)
        {
            var group = kvp.Key;
            var groupConfig = kvp.Value;

            if (group != vipInfo.Group)
                continue;

            foreach (var featureName in groupConfig.Features.Keys)
            {
                var featureState = VIPFeatureManager.GetPlayerFeatureState(player.SteamID, featureName);

                vipMenu.AddBoolOption(featureName, featureState == FeatureState.Enabled, (p, o) =>
                {
                    if (o is IT3Option boolOption)
                    {
                        bool isEnabled = boolOption.OptionDisplay!.Contains("✔");

                        if (isEnabled)
                        {
                            VIPFeatureManager.SetPlayerFeatureState(player.SteamID, featureName, FeatureState.Enabled);
                            p.SendChatLocalizedMessage("vip.FeatureEnabled", featureName);
                        }
                        else
                        {
                            VIPFeatureManager.SetPlayerFeatureState(player.SteamID, featureName, FeatureState.Disabled);
                            p.SendChatLocalizedMessage("vip.FeatureDisabled", featureName);
                        }
                    }
                });
            }
        }
        MenuManager.OpenMainMenu(player, vipMenu);
    }
}
