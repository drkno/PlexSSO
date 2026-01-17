using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Deluge.Plugin.Model;
using PlexSSO.Deluge.Plugin.Service.DelugeClient;
using PlexSSO.Plugin;
using PlexSSO.Service;
using PlexSSO.Service.Config;

namespace PlexSSO.Deluge.Plugin
{
    public class PluginDescriptor : IPlugin
    {
        public string Name { get; } = DelugeConstants.PluginName;

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ITokenService, DelugeTokenService>();
        }

        public void ConfigurePlugin(IConfigurationService configurationService)
        {
            var config = configurationService.GetPluginConfig<DelugeConfig>(DelugeConstants.PluginName);
            var publicHostname = configurationService.Config
                .TryGetPluginConfigValue(DelugeConstants.PluginName, DelugeConstants.PublicHostname);

            if (string.IsNullOrWhiteSpace(config.PublicHostname) &&
                !string.IsNullOrWhiteSpace(publicHostname))
            {
                config.PublicHostname = publicHostname;
                configurationService.SavePluginConfig(DelugeConstants.PluginName, config);
            }
        }
    }
}
