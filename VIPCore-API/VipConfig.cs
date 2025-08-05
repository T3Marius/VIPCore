namespace VipCoreApi;

public interface IVipCoreConfig
{
    public T LoadConfig<T>(string moduleName) where T : class, new();
    public void SaveConfig<T>(string moduleName, T config) where T : class, new();
}