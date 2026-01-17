using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Overseerr.Plugin.Model;
using PlexSSO.Overseerr.Plugin.OverseerrClient;
using PlexSSO.Plugin;
using PlexSSO.Service;
using PlexSSO.Service.Config;

namespace PlexSSO.Overseerr.Plugin
{
    public class PluginDescriptor : IPlugin
    {
        public string Name { get; } = OverseerrConstants.PluginName;

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ITokenService, OverseerrTokenService>();
        }

        public void ConfigurePlugin(IConfigurationService configurationService)
        {
            var config = configurationService.GetPluginConfig<OverseerrConfig>(OverseerrConstants.PluginName);
            var publicHostname = configurationService.Config
                .TryGetPluginConfigValue(OverseerrConstants.PluginName, OverseerrConstants.PublicHostname);

            if (string.IsNullOrWhiteSpace(config.PublicHostname) &&
                !string.IsNullOrWhiteSpace(publicHostname))
            {
                config.PublicHostname = publicHostname;
                configurationService.SavePluginConfig(OverseerrConstants.PluginName, config);
            }
        }
    }
}
