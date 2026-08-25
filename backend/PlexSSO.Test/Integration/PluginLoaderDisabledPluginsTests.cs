using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace PlexSSO.Test.Integration;

/// <summary>
/// Verifies that PlexSsoConfig.DisabledPlugins (config.json's "disabledPlugins") actually prevents
/// a discovered plugin's routes/endpoints from being registered at all, rather than merely making
/// them return an error - see PluginLoader.LoadPlugins.
///
/// Each test gets its own isolated config directory (via --config) so writing "disabledPlugins"
/// here cannot affect the shared config.json used by other integration tests.
///
/// Assertions are made directly against ASP.NET Core's ActionDescriptorCollection rather than by
/// issuing an HTTP request for an unmatched route: this application falls back to serving its SPA
/// for any unmatched route (see Startup.Configure -> UseSpa), so "not routed to a controller" does
/// not cleanly surface as an HTTP 404 - inspecting the registered controller actions directly is
/// both more precise and avoids depending on the SPA's build output being present.
/// </summary>
public class PluginLoaderDisabledPluginsTests
{
    private string _configDir;

    [SetUp]
    public void Setup()
    {
        _configDir = Path.Combine(Path.GetTempPath(), "plexsso-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_configDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_configDir))
        {
            Directory.Delete(_configDir, recursive: true);
        }
    }

    private static bool HasOidcControllerAction(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        return provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Any(action => action.ControllerName == "Oidc");
    }

    [Test]
    public async Task DisabledPlugins_ListsOidc_OidcControllerActionsAreNotRegistered()
    {
        File.WriteAllText(Path.Combine(_configDir, "config.json"), "{ \"disabledPlugins\": [\"oidc\"] }");

        var app = ProgramHost.BuildWebApplication(
            new[] { "--config", _configDir },
            builder => builder.WebHost.UseTestServer());
        await app.StartAsync();

        Assert.That(HasOidcControllerAction(app), Is.False,
            "OidcController's actions should not be registered when \"oidc\" is disabled");

        var healthcheck = await app.GetTestClient().GetAsync(Model.Constants.HealthcheckPath);
        Assert.That(healthcheck.IsSuccessStatusCode, Is.True,
            "rest of the application should still start normally");

        await app.StopAsync();
    }

    [Test]
    public async Task DisabledPlugins_Empty_OidcControllerActionsAreStillRegistered()
    {
        File.WriteAllText(Path.Combine(_configDir, "config.json"), "{ \"disabledPlugins\": [] }");

        var app = ProgramHost.BuildWebApplication(
            new[] { "--config", _configDir },
            builder => builder.WebHost.UseTestServer());
        await app.StartAsync();

        Assert.That(HasOidcControllerAction(app), Is.True);

        await app.StopAsync();
    }
}
