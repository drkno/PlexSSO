using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Model;
using PlexSSO.Model.Internal;
using PlexSSO.Extensions;

namespace PlexSSO
{
    public static class ProgramHost
    {
        public static WebApplication BuildWebApplication(string[] args, Action<WebApplicationBuilder> configureBuilder = null)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddCommandLine(args, typeof(PlexSsoConfig).GetAnnotatedCliArgumentsAsDictionary());

            // allow caller to customize the builder (e.g. use TestServer)
            configureBuilder?.Invoke(builder);

            // Configure web host defaults
            builder.WebHost.ConfigureKestrel((context, options) => options.AddServerHeader = false);
            builder.WebHost.UseUrls($"http://0.0.0.0:{Constants.PortNumber}/");

            // Instantiate Startup and register services
            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);

            var app = builder.Build();

            // Call Configure to register middleware
            var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
            startup.Configure(app, app.Environment, antiforgery);

            return app;
        }
    }
}
