using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PlexSSO.Extensions;
using PlexSSO.Model;
using PlexSSO.Model.Internal;
using PlexSSO.Service.HealthCheck;
using PlexSSO;

// Top-level statements - minimal hosting model
var argsList = args ?? [];

if (argsList.Contains("--healthcheck"))
{
    await HealthChecker.CheckHealth($"http://127.0.0.1:{Constants.PortNumber}{Constants.HealthcheckPath}");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Allow CLI args to map to our annotated config
builder.Configuration.AddCommandLine(argsList, typeof(PlexSsoConfig).GetAnnotatedCliArgumentsAsDictionary());

// Configure web host to use Startup for separation of concerns
builder.Host.ConfigureWebHostDefaults(webBuilder =>
{
    webBuilder.ConfigureKestrel((context, options) => options.AddServerHeader = false);
    webBuilder.UseUrls($"http://0.0.0.0:{Constants.PortNumber}/");
    webBuilder.UseStartup<Startup>();
});

var host = builder.Build();
await host.RunAsync();
