using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Service.Config;

namespace PlexSSO.Plugin
{
    public interface IPlugin
    {
        string Name { get; }
        void RegisterServices(IServiceCollection services);
        void ConfigurePlugin(IConfigurationService configurationService) {}
    }
}
