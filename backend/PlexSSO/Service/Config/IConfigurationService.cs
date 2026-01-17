using PlexSSO.Model.Internal;

namespace PlexSSO.Service.Config
{
    public interface IConfigurationService
    {
        PlexSsoConfig Config { get; }
        string GetConfigurationDirectory();
        T GetPluginConfig<T>(string key);
        void SavePluginConfig<T>(string key, T value);
    }
}
