using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Oidc.Plugin.Model;
using PlexSSO.Oidc.Plugin.Service;
using PlexSSO.Plugin;

namespace PlexSSO.Oidc.Plugin;

public class PluginDescriptor : IPlugin
{
    public string Name { get; } = OidcConstants.PluginName;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IOidcService, OidcService>();
    }
}
