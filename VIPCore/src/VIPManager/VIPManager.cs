using Microsoft.Extensions.Logging;
using VIPCore;
using VipCoreApi;

namespace VIPCore;
public static class VIPManager
{
    private static PluginConfig? _config;
    private static VipApi? _api;

    public static void Initialize(PluginConfig config, VipApi api)
    {
        _config = config;
        _api = api;
    }

    public static bool IsValidGroup(string groupName)
    {
        if (_config == null || string.IsNullOrEmpty(groupName))
            return false;

        return _config.Groups.ContainsKey(groupName);
    }

    public static IEnumerable<string> GetAvailableGroups()
    {
        return _config?.Groups.Keys ?? Enumerable.Empty<string>();
    }

    public static VIPGroup_Config? GetGroupConfig(string groupName)
    {
        if (_config == null || string.IsNullOrEmpty(groupName))
            return null;

        return _config.Groups.TryGetValue(groupName, out var config) ? config : null;
    }

    public static bool AddPlayerVip(ulong steamId, string group, TimeSpan duration)
    {
        if (_api == null)
        {
            VIPCore.Instance.Logger.LogError("VIP API not initialized");
            return false;
        }

        if (!IsValidGroup(group))
        {
            VIPCore.Instance.Logger.LogError($"Invalid VIP group: {group}. Available groups: {string.Join(", ", GetAvailableGroups())}");
            return false;
        }

        try
        {
            bool result = _api.AddPlayerVip(steamId, group, duration);
            if (result)
            {
                VIPCore.Instance.Logger.LogInformation($"Successfully added VIP status to SteamID {steamId} with group {group} for {duration}");
            }
            return result;
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to add VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static bool UpdatePlayerVip(ulong steamId, string newGroup, TimeSpan newDuration)
    {
        if (_api == null)
        {
            VIPCore.Instance.Logger.LogError("VIP API not initialized");
            return false;
        }

        if (!IsValidGroup(newGroup))
        {
            VIPCore.Instance.Logger.LogError($"Invalid VIP group: {newGroup}. Available groups: {string.Join(", ", GetAvailableGroups())}");
            return false;
        }

        try
        {
            bool result = _api.UpdatePlayerVip(steamId, newGroup, newDuration);
            if (result)
            {
                VIPCore.Instance.Logger.LogInformation($"Successfully updated VIP status for SteamID {steamId} to group {newGroup} for {newDuration}");
            }
            return result;
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to update VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static bool RemovePlayerVip(ulong steamId)
    {
        if (_api == null)
        {
            VIPCore.Instance.Logger.LogError("VIP API not initialized");
            return false;
        }

        try
        {
            bool result = _api.RemovePlayerVip(steamId);
            if (result)
            {
                VIPCore.Instance.Logger.LogInformation($"Successfully removed VIP status from SteamID {steamId}");
            }
            return result;
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to remove VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static bool IsPlayerVip(ulong steamId)
    {
        return _api?.IsPlayerVip(steamId) ?? false;
    }

    public static VipInfo? GetPlayerVipInfo(ulong steamId)
    {
        return _api?.GetPlayerVipInfo(steamId);
    }
}