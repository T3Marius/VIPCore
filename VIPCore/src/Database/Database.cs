using CounterStrikeSharp.API.Core;
using MySqlConnector;
using Dapper;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using VipCoreApi;

namespace VIPCore;

public static class Database
{
    public static string DatabaseConnectionString = string.Empty;

    private static async Task<MySqlConnection> ConnectAsync()
    {
        if (string.IsNullOrEmpty(DatabaseConnectionString))
        {
            throw new InvalidOperationException("Database connection string is not set.");
        }
        MySqlConnection connection = new MySqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static void ExecuteAsync(string query, object? parameters)
    {
        Task.Run(async () =>
        {
            try
            {
                using MySqlConnection connection = await ConnectAsync();
                await connection.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                VIPCore.Instance.Logger.LogError($"Failed to execute query: {ex.Message}");
            }
        });
    }

    private static MySqlSslMode FromSslMode(string sslMode)
    {
        MySqlSslMode mode = sslMode.ToLower() switch
        {
            "none" => MySqlSslMode.None,
            "preferred" => MySqlSslMode.Preferred,
            "required" => MySqlSslMode.Required,
            "verifyca" => MySqlSslMode.VerifyCA,
            "verifyfull" => MySqlSslMode.VerifyFull,
            _ => throw new ArgumentException($"Invalid SSL mode: {sslMode}")
        };
        return mode;
    }

    public static async Task InitializeAsync(Database_Config config)
    {
        await CreateDatabaseAsync(config);
    }

    private static async Task CreateDatabaseAsync(Database_Config config)
    {
        try
        {
            MySqlConnectionStringBuilder builder = new()
            {
                Server = config.Host,
                Database = config.Name,
                UserID = config.User,
                Password = config.Pass,
                Port = config.Port,
                SslMode = FromSslMode(config.SslMode),
                MinimumPoolSize = 0,
                MaximumPoolSize = 64,
                Pooling = true
            };

            DatabaseConnectionString = builder.ConnectionString;

            using MySqlConnection connection = await ConnectAsync();
            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS vip_users (
                        id INT NOT NULL AUTO_INCREMENT,
                        SteamID BIGINT UNSIGNED NOT NULL,
                        PlayerName VARCHAR(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                        `Group` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                        DateAdded DATETIME NOT NULL,
                        DateExpire DATETIME NOT NULL,
                        PRIMARY KEY (id),
                        UNIQUE KEY id (id),
                        UNIQUE KEY SteamID (SteamID)
                );", transaction: transaction);

                await connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS vip_player_features (
                        id INT NOT NULL AUTO_INCREMENT,
                        SteamID BIGINT UNSIGNED NOT NULL,
                        FeatureName VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                        FeatureState TINYINT NOT NULL DEFAULT 1,
                        PRIMARY KEY (id),
                        UNIQUE KEY unique_player_feature (SteamID, FeatureName),
                        INDEX idx_steamid (SteamID)
                );", transaction: transaction);

                await connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS free_vips (
                        id INT NOT NULL AUTO_INCREMENT,
                        SteamID BIGINT UNSIGNED NOT NULL,
                        GroupName VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                        PlayerName VARCHAR(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                        DateAdded DATETIME NOT NULL,
                        DateExpire DATETIME NOT NULL,
                        PRIMARY KEY (id),
                        UNIQUE KEY unique_player_group (SteamID, GroupName),
                        INDEX idx_steamid (SteamID)
                );", transaction: transaction);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                VIPCore.Instance.Logger.LogError($"Failed to create table: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to create database connection: {ex.Message}");
            throw;
        }
    }
    public static async Task RemoveExpiredVipsAsync()
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();
            int rowsAffected = await connection.ExecuteAsync(@"
                DELETE FROM vip_users WHERE DateExpire <= @Now",
                new { Now = DateTime.UtcNow });

            if (rowsAffected > 0)
            {
                VIPCore.Instance.Logger.LogInformation($"Removed {rowsAffected} expired VIP players.");
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to remove expired VIPs: {ex.Message}");
        }
    }
    private static async Task<bool> AddVipAsync(ulong steamId, string group, TimeSpan duration)
    {
        if (string.IsNullOrEmpty(group))
        {
            throw new ArgumentException("Group cannot be null or empty.", nameof(group));
        }

        string playerName;
        CCSPlayerController? player = Utilities.GetPlayerFromSteamId(steamId);

        if (player != null && player.Connected == PlayerConnectedState.PlayerConnected)
        {
            playerName = player.PlayerName;
        }
        else
        {
            playerName = await Lib.GetPlayerNameFromSteamID(steamId);
        }
        try
        {
            using MySqlConnection connection = await ConnectAsync();
            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            DateTime dateAdded = DateTime.UtcNow;
            DateTime dateExpire = dateAdded.Add(duration);

            try
            {
                int rowsAffected = await connection.ExecuteAsync(@"
            INSERT INTO vip_users (SteamID, PlayerName, `Group`, DateAdded, DateExpire)
            VALUES (@SteamID, @PlayerName, @Group, @DateAdded, @DateExpire)",
                    new
                    {
                        SteamID = steamId,
                        PlayerName = playerName,
                        Group = group,
                        DateAdded = dateAdded,
                        DateExpire = dateExpire
                    }, transaction: transaction);

                await transaction.CommitAsync();
                return rowsAffected > 0;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                await transaction.RollbackAsync();
                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                VIPCore.Instance.Logger.LogError($"Failed to add VIP for SteamID {steamId}: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Database connection failed while adding VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> UpdateVipAsync(ulong steamId, string group, TimeSpan duration)
    {
        if (string.IsNullOrEmpty(group))
        {
            throw new ArgumentException("Group cannot be null or empty.", nameof(group));
        }

        try
        {
            using MySqlConnection connection = await ConnectAsync();
            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            try
            {
                DateTime dateAdded = DateTime.UtcNow;
                DateTime dateExpire = dateAdded.Add(duration);

                int rowsAffected = await connection.ExecuteAsync(@"
                UPDATE vip_users 
                SET `Group` = @Group, DateAdded = @DateAdded, DateExpire = @DateExpire
                WHERE SteamID = @SteamID AND DateExpire > @Now",
                    new
                    {
                        Group = group,
                        DateAdded = dateAdded,
                        DateExpire = dateExpire,
                        SteamID = steamId,
                        Now = DateTime.UtcNow
                    }, transaction: transaction);

                await transaction.CommitAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                VIPCore.Instance.Logger.LogError($"Failed to update VIP for SteamID {steamId}: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Database connection failed while updating VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> RemoveVipAsync(ulong steamId)
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();

            int rowsAffected = await connection.ExecuteAsync(@"
            DELETE FROM vip_users WHERE SteamID = @SteamID",
                new { SteamID = steamId });

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to remove VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> IsVipAsync(ulong steamId)
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();

            var result = await connection.QueryFirstOrDefaultAsync<DateTime?>(@"
                SELECT DateExpire FROM vip_users WHERE SteamID = @SteamID",
                new { SteamID = steamId });

            return result.HasValue && result.Value > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to check VIP status for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    private static async Task<VipInfo?> GetVipInfoAsync(ulong steamId)
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();

            var result = await connection.QueryFirstOrDefaultAsync(@"
                SELECT `Group`, DateExpire FROM vip_users WHERE SteamID = @SteamID",
                new { SteamID = steamId });

            if (result == null)
                return null;

            if (result.DateExpire <= DateTime.UtcNow)
                return null;

            return new VipInfo
            {
                Group = result.Group,
                ExpiryTimestamp = ((DateTimeOffset)result.DateExpire).ToUnixTimeSeconds()
            };
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get VIP info for SteamID {steamId}: {ex.Message}");
            return null;
        }
    }
    public static async Task<FeatureState> GetPlayerFeatureStateAsync(ulong steamId, string featureName)
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();

            var result = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT FeatureState FROM vip_player_features 
                WHERE SteamID = @SteamID AND FeatureName = @FeatureName",
                new { SteamID = steamId, FeatureName = featureName });

            if (result.HasValue)
            {
                return (FeatureState)result.Value;
            }

            return FeatureState.Enabled; // Default to enabled if no preference set
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get feature state for SteamID {steamId}, feature {featureName}: {ex.Message}");
            return FeatureState.Enabled;
        }
    }

    public static async Task<bool> SetPlayerFeatureStateAsync(ulong steamId, string featureName, FeatureState state)
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();
            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            try
            {
                int rowsAffected = await connection.ExecuteAsync(@"
                    INSERT INTO vip_player_features (SteamID, FeatureName, FeatureState)
                    VALUES (@SteamID, @FeatureName, @FeatureState)
                    ON DUPLICATE KEY UPDATE FeatureState = @FeatureState",
                    new
                    {
                        SteamID = steamId,
                        FeatureName = featureName,
                        FeatureState = (int)state
                    }, transaction: transaction);

                await transaction.CommitAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                VIPCore.Instance.Logger.LogError($"Failed to set feature state for SteamID {steamId}, feature {featureName}: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Database connection failed while setting feature state for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static async Task<Dictionary<string, FeatureState>> GetPlayerFeatureStatesAsync(ulong steamId)
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();

            var results = await connection.QueryAsync(@"
                SELECT FeatureName, FeatureState FROM vip_player_features 
                WHERE SteamID = @SteamID",
                new { SteamID = steamId });

            var featureStates = new Dictionary<string, FeatureState>();
            foreach (var result in results)
            {
                featureStates[result.FeatureName] = (FeatureState)result.FeatureState;
            }

            return featureStates;
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get feature states for SteamID {steamId}: {ex.Message}");
            return new Dictionary<string, FeatureState>();
        }
    }
    private static async Task<bool> HasReceivedFreeVipAsync(ulong steamId, string group)
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();

            var result = await connection.QueryFirstOrDefaultAsync<int>(@"
                SELECT COUNT(*) FROM free_vips 
                WHERE SteamID = @SteamID AND GroupName = @GroupName",
                new { SteamID = steamId, GroupName = group });

            return result > 0;
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to check free VIP history for SteamID {steamId}: {ex.Message}");
            return true; // Return true to prevent giving VIP on error
        }
    }

    private static async Task<bool> AddFreeVipRecordAsync(ulong steamId, string group, TimeSpan duration)
    {
        if (string.IsNullOrEmpty(group))
        {
            throw new ArgumentException("Group cannot be null or empty.", nameof(group));
        }

        // Double-check if already received to prevent race conditions
        if (await HasReceivedFreeVipAsync(steamId, group))
        {
            return false; // Already received, don't add again
        }

        string playerName;
        CCSPlayerController? player = Utilities.GetPlayerFromSteamId(steamId);

        if (player != null && player.Connected == PlayerConnectedState.PlayerConnected)
        {
            playerName = player.PlayerName;
        }
        else
        {
            playerName = "Unknown Player";
        }

        try
        {
            using MySqlConnection connection = await ConnectAsync();
            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            DateTime dateAdded = DateTime.UtcNow;
            DateTime dateExpire = dateAdded.Add(duration);

            try
            {
                // Use INSERT IGNORE to prevent duplicate key errors
                int rowsAffected = await connection.ExecuteAsync(@"
                    INSERT IGNORE INTO free_vips (SteamID, GroupName, PlayerName, DateAdded, DateExpire)
                    VALUES (@SteamID, @GroupName, @PlayerName, @DateAdded, @DateExpire)",
                    new
                    {
                        SteamID = steamId,
                        GroupName = group,
                        PlayerName = playerName,
                        DateAdded = dateAdded,
                        DateExpire = dateExpire
                    }, transaction: transaction);

                await transaction.CommitAsync();

                // If rowsAffected is 0, it means the record already existed
                if (rowsAffected == 0)
                {
                    VIPCore.Instance.Logger.LogWarning($"Free VIP record already exists for SteamID {steamId} and group {group}");
                    return false;
                }

                return true;
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate entry error
            {
                await transaction.RollbackAsync();
                VIPCore.Instance.Logger.LogWarning($"Duplicate free VIP record attempted for SteamID {steamId} and group {group}");
                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                VIPCore.Instance.Logger.LogError($"Failed to add free VIP record for SteamID {steamId}: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Database connection failed while adding free VIP record for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static async Task RemoveExpiredFreeVipsAsync()
    {
        try
        {
            using MySqlConnection connection = await ConnectAsync();
            int rowsAffected = await connection.ExecuteAsync(@"
                DELETE FROM free_vips WHERE DateExpire <= @Now",
                new { Now = DateTime.UtcNow });

            if (rowsAffected > 0)
            {
                VIPCore.Instance.Logger.LogInformation($"Removed {rowsAffected} expired free VIP records.");
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to remove expired Free VIPs: {ex.Message}");
        }
    }

    private static async Task<List<string>> GetAvailableFreeVipGroupsAsync(ulong steamId)
    {
        var allGroups = new List<string> { "SILVER-VIP", "GOLD-VIP", "PLATINUM-VIP" };
        var availableGroups = new List<string>();

        try
        {
            foreach (var group in allGroups)
            {
                if (!await HasReceivedFreeVipAsync(steamId, group))
                {
                    availableGroups.Add(group);
                }
            }
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get available free VIP groups for SteamID {steamId}: {ex.Message}");
        }

        return availableGroups;
    }

    public static bool HasReceivedFreeVip(ulong steamId, string group)
    {
        try
        {
            return HasReceivedFreeVipAsync(steamId, group).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to check free VIP history for SteamID {steamId}: {ex.Message}");
            return true;
        }
    }

    public static bool AddFreeVipRecord(ulong steamId, string group, TimeSpan duration)
    {
        try
        {
            return AddFreeVipRecordAsync(steamId, group, duration).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to add free VIP record for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static List<string> GetAvailableFreeVipGroups(ulong steamId)
    {
        try
        {
            return GetAvailableFreeVipGroupsAsync(steamId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get available free VIP groups for SteamID {steamId}: {ex.Message}");
            return new List<string>();
        }
    }
    // Synchronous wrappers for feature states
    public static FeatureState GetPlayerFeatureState(ulong steamId, string featureName)
    {
        try
        {
            return GetPlayerFeatureStateAsync(steamId, featureName).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get feature state for SteamID {steamId}, feature {featureName}: {ex.Message}");
            return FeatureState.Enabled;
        }
    }

    public static bool SetPlayerFeatureState(ulong steamId, string featureName, FeatureState state)
    {
        try
        {
            return SetPlayerFeatureStateAsync(steamId, featureName, state).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to set feature state for SteamID {steamId}, feature {featureName}: {ex.Message}");
            return false;
        }
    }

    public static Dictionary<string, FeatureState> GetPlayerFeatureStates(ulong steamId)
    {
        try
        {
            return GetPlayerFeatureStatesAsync(steamId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get feature states for SteamID {steamId}: {ex.Message}");
            return new Dictionary<string, FeatureState>();
        }
    }
    public static bool IsVip(ulong steamId)
    {
        try
        {
            return IsVipAsync(steamId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to check VIP status for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static bool AddVip(ulong steamId, string group, TimeSpan duration)
    {
        try
        {
            if (IsVip(steamId))
            {
                VIPCore.Instance.Logger.LogWarning($"Player with SteamID {steamId} already has VIP status.");
                return false;
            }

            return AddVipAsync(steamId, group, duration).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to add VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static bool UpdateVip(ulong steamId, string group, TimeSpan duration)
    {
        try
        {
            if (!IsVip(steamId))
            {
                VIPCore.Instance.Logger.LogWarning($"Player with SteamID {steamId} is not a VIP.");
                return false;
            }

            return UpdateVipAsync(steamId, group, duration).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to update VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static bool RemoveVip(ulong steamId)
    {
        try
        {
            if (!IsVip(steamId))
            {
                VIPCore.Instance.Logger.LogWarning($"Player with SteamID {steamId} is not a VIP.");
                return false;
            }

            return RemoveVipAsync(steamId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to remove VIP for SteamID {steamId}: {ex.Message}");
            return false;
        }
    }

    public static VipInfo? GetVipInfo(ulong steamId)
    {
        try
        {
            return GetVipInfoAsync(steamId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError($"Failed to get VIP info for SteamID {steamId}: {ex.Message}");
            return null;
        }
    }
}