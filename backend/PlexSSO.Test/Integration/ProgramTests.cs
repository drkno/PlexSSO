using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using PlexSSO.Model;

namespace PlexSSO.Test.Integration
{
    public class ProgramTests
    {
        [Test]
        public async Task Program_BuildWebApplication_Starts_App_And_Healthcheck_Returns_OK()
        {
            // Build app using the same wiring as Program via ProgramHost, but use TestServer
            var app = ProgramHost.BuildWebApplication([], builder => builder.WebHost.UseTestServer());

            await app.StartAsync();

            var client = app.GetTestClient();
            var response = await client.GetAsync(Constants.HealthcheckPath);

            Assert.That(response.IsSuccessStatusCode, Is.True);

            await app.StopAsync();
        }
    }
}
