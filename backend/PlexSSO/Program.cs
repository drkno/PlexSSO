using System;
using System.Linq;
using PlexSSO.Model;
using PlexSSO.Service.HealthCheck;
using PlexSSO;

// Top-level statements - minimal hosting model
var argsList = args ?? [];

if (argsList.Contains("--healthcheck"))
{
    await HealthChecker.CheckHealth($"http://127.0.0.1:{Constants.PortNumber}{Constants.HealthcheckPath}");
    return;
}

var app = ProgramHost.BuildWebApplication(args);
await app.RunAsync();
