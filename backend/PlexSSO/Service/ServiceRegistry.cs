using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Model.Internal;
using PlexSSO.Service.Auth;
using PlexSSO.Service.Config;
using PlexSSO.Service.OmbiClient;
using PlexSSO.Service.OverseerrClient;
using PlexSSO.Service.PlexClient;
using PlexSSO.Service.TautulliClient;

namespace PlexSSO.Service
{
    public static class ServiceRegistry
    {
        public static IConfigurationService<PlexSsoConfig> LoadConfiguration(IConfiguration configuration)
        {
            return new ConfigurationService<PlexSsoConfig>(configuration);
        }

        public static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<IPlexClient, PlexHttpClient>();
            services.AddSingleton<IAuthValidator, AuthenticationValidator>();
            services.AddSingleton<ITokenService, OmbiTokenService>();
            services.AddSingleton<ITokenService, OverseerrTokenService>();
            services.AddSingleton<ITokenService, TautulliTokenService>();
        }
    }
}
