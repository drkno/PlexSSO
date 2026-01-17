using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace PlexSSO.Test.Integration
{
    public class OidcIntegrationTests
    {
        [Test]
        public async Task Get_OpenIdConfiguration_Returns_Json_With_Issuer()
        {
            var app = ProgramHost.BuildWebApplication(new string[0], builder => builder.WebHost.UseTestServer());
            await app.StartAsync();
            var client = app.GetTestClient();

            var response = await client.GetAsync("/.well-known/openid-configuration");
            var content = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Does.Contain("issuer"));
            Assert.That(content, Does.Contain("authorization_endpoint"));
            Assert.That(content, Does.Contain("jwks_uri"));

            await app.StopAsync();
        }

        [Test]
        public async Task Get_Jwks_Returns_Keys()
        {
            var app = ProgramHost.BuildWebApplication(new string[0], builder => builder.WebHost.UseTestServer());
            await app.StartAsync();
            var client = app.GetTestClient();

            var response = await client.GetAsync("/oidc/jwks");
            var content = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Does.Contain("keys"));
            Assert.That(content, Does.Contain("kid"));

            await app.StopAsync();
        }

        [Test]
        public async Task Get_Authorize_Redirects_To_Login_If_Not_Authenticated()
        {
            var app = ProgramHost.BuildWebApplication(new string[0], builder => builder.WebHost.UseTestServer());
            await app.StartAsync();
            var client = app.GetTestClient();

            var response = await client.GetAsync("/oidc/authorize?response_type=code&client_id=jellyfin&redirect_uri=http://localhost:8096&state=xyz&nonce=123");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            // Should redirect to login with returnUrl
            var location = response.Headers.Location.ToString();
            Assert.That(location, Does.StartWith("/sso/login"));
            Assert.That(location, Does.Contain("returnUrl="));

            await app.StopAsync();
        }
    }
}
