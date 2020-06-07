using static PlexSSO.Service.Config.PlexSsoConfig;

namespace PlexSSO.Service.Config
{
    public interface IConfigurationService
    {
        PlexSsoConfig GetConfig();
        AccessControl[] GetAccessControls(string serviceName);
    }
}
