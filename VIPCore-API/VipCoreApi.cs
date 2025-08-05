using CounterStrikeSharp.API.Core.Capabilities;


namespace VipCoreApi;

public class VipInfo
{
    public required string Group { get; init; }
    public long ExpiryTimestamp { get; init; }
}
public enum FeatureState
{
    Disabled,
    Enabled,
    Deactivated // Feature disabled from config, ignore.
}

public class PlayerFeatureState
{
    public ulong SteamID { get; set; }
    public Dictionary<string, FeatureState> Features { get; set; } = new();
}
public interface IVipFeatureBase
{
    string FeatureName { get; }
    void OnFeatureLoaded();
}

public interface IVipCoreApi
{
    public static PluginCapability<IVipCoreApi> Capability { get; } = new("vip:core");

    /// <summary>
    /// Checks if a player currently has VIP status.
    /// </summary>
    public bool IsPlayerVip(ulong steamId);

    /// <summary>
    /// Gets the VIP information for a player.
    /// </summary>
    public VipInfo? GetPlayerVipInfo(ulong steamId);

    /// <summary>
    /// Adds VIP status to a player who is not currently a VIP.
    /// Fails if the player already has VIP.
    /// </summary>
    public bool AddPlayerVip(ulong steamId, string group, TimeSpan duration);

    /// <summary>
    /// Updates the group and/or duration for a player who is already a VIP.
    /// Fails if the player is not currently a VIP.
    /// </summary>
    public bool UpdatePlayerVip(ulong steamId, string newGroup, TimeSpan newDuration);

    /// <summary>
    /// Removes VIP status from a player.
    /// </summary>
    public bool RemovePlayerVip(ulong steamId);

    /// <summary>
    /// Gets the feature state for a specific player and feature
    /// </summary>
    public FeatureState GetPlayerFeatureState(ulong steamId, string featureName);

    /// <summary>
    /// Sets the feature state for a specific player and feature
    /// </summary>
    public bool SetPlayerFeatureState(ulong steamId, string featureName, FeatureState state);

    /// <summary>
    /// Gets all feature states for a player
    /// </summary>
    public Dictionary<string, FeatureState> GetPlayerFeatureStates(ulong steamId);

    /// <summary>
    /// Checks if a feature is enabled for a player (considers both group config and player preference)
    /// </summary>
    public bool IsPlayerFeatureEnabled(ulong steamId, string featureName);

    /// <summary>
    /// Gets the feature value for a player (returns null if feature not available or disabled)
    /// </summary>
    public object? GetPlayerFeatureValue(ulong steamId, string featureName);

    /// <summary>
    /// Registers a VIP feature
    /// </summary>
    public void RegisterFeature(IVipFeatureBase feature);

    /// <summary>
    /// Unregisters a VIP feature
    /// </summary>
    public void UnregisterFeature(string featureName);

    /// <summary>
    /// Gets all registered feature names
    /// </summary>
    public IEnumerable<string> GetRegisteredFeatures();

    /// <summary>
    /// Load a module's configuration (creates it if it doesn't exits)
    /// </summary>
    public T GetModuleConfig<T>(string moduleName) where T : class, new();

    /// <summary>
    /// Save a module's configuration
    /// </summary>
    public void SaveModuleConfig<T>(string moduleName, T config) where T : class, new();

    /// <summary>
    /// Gets a simple feature value (int, bool, string, etc)
    /// </summary>
    public T? GetFeatureValue<T>(ulong steamId, string featureName) where T : struct;

    /// <summary>
    /// Gets a] complex feature configuration object
    /// </summary>
    public T? GetFeatureConfig<T>(ulong steamId, string featureName) where T : class, new();
}