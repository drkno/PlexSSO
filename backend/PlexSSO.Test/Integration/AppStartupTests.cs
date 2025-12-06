using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using PlexSSO.Model;

namespace PlexSSO.Test.Integration;

public class AppStartupTests
{
    [Test]
    public async Task App_Starts_And_Healthcheck_Returns_OK()
    {
        await using var factory = new WebApplicationFactory<Startup>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Constants.HealthcheckPath);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
