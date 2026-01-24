using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

        services.AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOidcService>((options, oidcService) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = oidcService.GetSigningKey()
                };
            });
    }
}
