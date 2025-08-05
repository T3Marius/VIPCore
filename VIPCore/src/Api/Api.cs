using CounterStrikeSharp.API.Core;
using System.Text.Json;
using VipCoreApi;

namespace VIPCore;

public class VipApi : IVipCoreApi
{
    public VipCoreConfig? _configProvider;
    private string? _configPath;

    public VipApi()
    {
    }

    public VipApi(string configPath)
    {
        _configPath = configPath;
        _configProvider = new VipCoreConfig(configPath);
    }

    public void SetConfigPath(string configPath)
    {
        _configPath = configPath;
        _configProvider = new VipCoreConfig(configPath);
    }

    public bool IsPlayerVip(ulong steamId)
    {
        return Database.IsVip(steamId);
    }

    public VipInfo? GetPlayerVipInfo(ulong steamId)
    {
        return Database.GetVipInfo(steamId);
    }

    public bool AddPlayerVip(ulong steamId, string group, TimeSpan duration)
    {
        return Database.AddVip(steamId, group, duration);
    }

    public bool UpdatePlayerVip(ulong steamId, string newGroup, TimeSpan newDuration)
    {
        return Database.UpdateVip(steamId, newGroup, newDuration);
    }

    public bool RemovePlayerVip(ulong steamId)
    {
        return Database.RemoveVip(steamId);
    }

    public FeatureState GetPlayerFeatureState(ulong steamId, string featureName)
    {
        return VIPFeatureManager.GetPlayerFeatureState(steamId, featureName);
    }

    public bool SetPlayerFeatureState(ulong steamId, string featureName, FeatureState state)
    {
        return VIPFeatureManager.SetPlayerFeatureState(steamId, featureName, state);
    }

    public Dictionary<string, FeatureState> GetPlayerFeatureStates(ulong steamId)
    {
        return VIPFeatureManager.GetPlayerFeatureStates(steamId);
    }

    public bool IsPlayerFeatureEnabled(ulong steamId, string featureName)
    {
        return VIPFeatureManager.IsPlayerFeatureEnabled(steamId, featureName);
    }

    public object? GetPlayerFeatureValue(ulong steamId, string featureName)
    {
        return VIPFeatureManager.GetPlayerFeatureValue(steamId, featureName);
    }

    public void RegisterFeature(IVipFeatureBase feature)
    {
        VIPFeatureManager.RegisterFeature(feature);
    }

    public void UnregisterFeature(string featureName)
    {
        VIPFeatureManager.UnregisterFeature(featureName);
    }

    public IEnumerable<string> GetRegisteredFeatures()
    {
        return VIPFeatureManager.GetRegisteredFeatures();
    }

    public T GetModuleConfig<T>(string moduleName) where T : class, new()
    {
        return _configProvider!.LoadConfig<T>(moduleName) ?? new T();
    }

    public void SaveModuleConfig<T>(string moduleName, T config) where T : class, new()
    {
        _configProvider!.SaveConfig(moduleName, config);
    }
    public T? GetFeatureValue<T>(ulong steamId, string featureName) where T : struct
    {
        object? featureValue = GetPlayerFeatureValue(steamId, featureName);
        if (featureValue == null)
            return null;

        try
        {
            if (featureValue is JsonElement jsonElement)
            {
                return typeof(T) switch
                {
                    var t when t == typeof(int) => (T)(object)jsonElement.GetInt32(),
                    var t when t == typeof(bool) => (T)(object)jsonElement.GetBoolean(),
                    var t when t == typeof(float) => (T)(object)jsonElement.GetSingle(),
                    var t when t == typeof(double) => (T)(object)jsonElement.GetDouble(),
                    var t when t == typeof(long) => (T)(object)jsonElement.GetInt64(),
                    _ => default(T)
                };
            }

            if (featureValue is T directValue)
                return directValue;


            return (T)Convert.ChangeType(featureValue, typeof(T));
        }
        catch
        {
            return null;
        }
    }
    public T? GetFeatureConfig<T>(ulong steamId, string featureName) where T : class, new()
    {
        object? featureValue = GetPlayerFeatureValue(steamId, featureName);
        if (featureValue == null)
            return null;

        try
        {
            if (featureValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }

            if (featureValue is string jsonString)
            {
                return JsonSerializer.Deserialize<T>(jsonString);
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}