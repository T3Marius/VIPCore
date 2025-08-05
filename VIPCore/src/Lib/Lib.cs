using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Net.Http;
using static VIPCore.VIPCore;

namespace VIPCore;

public static class Lib
{
    private static readonly HttpClient _httpClient = new();
    public static void SendChatLocalizedMessage(this CCSPlayerController player, string key, params object[] args)
    {
        if (LangApi == null)
            return;

        var message = LangApi.GetPlayerTranslation(player, Instance.ModuleName, key, args);
        var prefix = LangApi.GetPlayerTranslation(player, Instance.ModuleName, "prefix");

        player.PrintToChat(prefix + message);
    }
    public static void SendReplyLocalizedMessage(this CCSPlayerController player, CommandInfo info, string key, params object[] args)
    {
        if (LangApi == null)
            return;

        var message = LangApi.GetPlayerTranslation(player, Instance.ModuleName, key, args);
        var prefix = LangApi.GetPlayerTranslation(player, Instance.ModuleName, "prefix");
        info.ReplyToCommand(prefix + message);
    }

    public static async Task<string> GetPlayerNameFromSteamID(ulong steamID)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync($"https://steamcommunity.com/profiles/{steamID}/?xml=1");
            response.EnsureSuccessStatusCode();

            string xmlContent = await response.Content.ReadAsStringAsync();

            System.Xml.XmlDocument xmlDoc = new();
            xmlDoc.LoadXml(xmlContent);

            System.Xml.XmlNode? nameNode = xmlDoc.SelectSingleNode("//steamID");

            string? name = nameNode?.InnerText.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return steamID.ToString();
            }

            return name;
        }
        catch (Exception)
        {
            return steamID.ToString();
        }
    }
}