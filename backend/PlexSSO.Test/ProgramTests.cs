using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlexSSO.Model;

namespace PlexSSO.Test
{
    public class ProgramTests
    {
        [Test]
        public async Task Program_Wiring_Starts_App_And_Healthcheck_Returns_OK()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Development,
                ApplicationName = typeof(PlexSSO.Startup).Assembly.GetName().Name
            });

            // Use in-memory TestServer to avoid binding ports
            builder.WebHost.UseTestServer();

            // Wire up services via Startup
            var startup = new PlexSSO.Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);

            var app = builder.Build();

            // Call Configure to register middleware and endpoints
            var antiforgery = app.Services.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            startup.Configure(app, app.Environment, antiforgery);

            await app.StartAsync();

            var client = app.GetTestClient();
            var response = await client.GetAsync(Constants.HealthcheckPath);

            Assert.That(response.IsSuccessStatusCode, Is.True);

            await app.StopAsync();
        }
    }
}
