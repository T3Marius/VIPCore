using Microsoft.Extensions.Logging;
using VIPCore;
using VipCoreApi;

namespace VIPCore;

public static class VIPFeatureManager
{
    private static readonly Dictionary<string, IVipFeatureBase> _registeredFeatures = new();

    public static void RegisterFeature(IVipFeatureBase feature)
    {
        _registeredFeatures[feature.FeatureName] = feature;
        feature.OnFeatureLoaded();

        VIPCore.Instance.Logger.LogInformation("VIP Feature '{0}' registered successfully", feature.FeatureName);
    }

    public static void UnregisterFeature(string featureName)
    {
        if (_registeredFeatures.Remove(featureName))
        {
            VIPCore.Instance.Logger.LogInformation("VIP Feature '{0}' unregistered", featureName);
        }
    }

    public static IVipFeatureBase? GetFeature(string featureName)
    {
        return _registeredFeatures.TryGetValue(featureName, out var feature) ? feature : null;
    }

    public static IEnumerable<string> GetRegisteredFeatures()
    {
        return _registeredFeatures.Keys;
    }

    public static FeatureState GetPlayerFeatureState(ulong steamId, string featureName)
    {
        return Database.GetPlayerFeatureState(steamId, featureName);
    }

    public static bool SetPlayerFeatureState(ulong steamId, string featureName, FeatureState state)
    {
        return Database.SetPlayerFeatureState(steamId, featureName, state);
    }

    public static Dictionary<string, FeatureState> GetPlayerFeatureStates(ulong steamId)
    {
        return Database.GetPlayerFeatureStates(steamId);
    }

    public static bool IsPlayerFeatureEnabled(ulong steamId, string featureName)
    {
        var vipInfo = VIPManager.GetPlayerVipInfo(steamId);
        if (vipInfo == null) return false;

        var groupConfig = VIPManager.GetGroupConfig(vipInfo.Group);
        if (groupConfig == null || !groupConfig.Features.ContainsKey(featureName))
            return false;

        var playerState = GetPlayerFeatureState(steamId, featureName);
        return playerState == FeatureState.Enabled;
    }

    public static object? GetPlayerFeatureValue(ulong steamId, string featureName)
    {
        if (!IsPlayerFeatureEnabled(steamId, featureName))
            return null;

        var vipInfo = VIPManager.GetPlayerVipInfo(steamId);
        if (vipInfo == null) return null;

        var groupConfig = VIPManager.GetGroupConfig(vipInfo.Group);
        return groupConfig?.Features.TryGetValue(featureName, out var value) == true ? value : null;
    }
}