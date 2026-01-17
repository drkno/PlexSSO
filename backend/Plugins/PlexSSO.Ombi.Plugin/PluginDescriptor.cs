using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Ombi.Plugin.Model;
using PlexSSO.Ombi.Plugin.Service.OmbiClient;
using PlexSSO.Plugin;
using PlexSSO.Service;
using PlexSSO.Service.Config;

namespace PlexSSO.Ombi.Plugin
{
    public class PluginDescriptor : IPlugin
    {
        public string Name { get; } = OmbiConstants.PluginName;

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ITokenService, OmbiTokenService>();
        }

        public void ConfigurePlugin(IConfigurationService configurationService)
        {
            var config = configurationService.GetPluginConfig<OmbiConfig>(OmbiConstants.PluginName);
            var publicHostname = configurationService.Config
                .TryGetPluginConfigValue(OmbiConstants.PluginName, OmbiConstants.PublicHostname);

            if (string.IsNullOrWhiteSpace(config.PublicHostname) &&
                !string.IsNullOrWhiteSpace(publicHostname))
            {
                config.PublicHostname = publicHostname;
                configurationService.SavePluginConfig(OmbiConstants.PluginName, config);
            }
        }
    }
}
