using Tomlet;
using Microsoft.Extensions.Logging;
using VipCoreApi;

namespace VIPCore;

public class VipCoreConfig : IVipCoreConfig
{
    private readonly string _configDirectory;
    private readonly string _modulesDirectory;

    public VipCoreConfig(string baseConfigPath)
    {
        _configDirectory = Path.GetDirectoryName(baseConfigPath) ?? "";
        _modulesDirectory = Path.Combine(_configDirectory, "Modules");
    }
    public T LoadConfig<T>(string moduleName) where T : class, new()
    {
        string configPath = Path.Combine(_modulesDirectory, $"{moduleName}.toml");
        if (!File.Exists(configPath))
        {
            var defaultConfig = new T();
            SaveConfig(moduleName, defaultConfig);
            return defaultConfig;
        }

        try
        {
            using var fs = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);

            string fileContent = sr.ReadToEnd();
            return TomletMain.To<T>(fileContent);
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError("Error loading module {0} config: {1}", moduleName, ex.Message);
            VIPCore.Instance.Logger.LogWarning("Fallback to default values for this config to prevent crashing.");
            return new T();
        }
    }
    public void SaveConfig<T>(string moduleName, T config) where T : class, new()
    {
        string configPath = Path.Combine(_modulesDirectory, $"{moduleName}.toml");
        try
        {
            Directory.CreateDirectory(_modulesDirectory);

            string tomlContent = TomletMain.TomlStringFrom(config);
            File.WriteAllText(configPath, tomlContent);
        }
        catch (Exception ex)
        {
            VIPCore.Instance.Logger.LogError("Failed to save module {0} config: {1}", moduleName, ex.Message);
        }
    }
}
