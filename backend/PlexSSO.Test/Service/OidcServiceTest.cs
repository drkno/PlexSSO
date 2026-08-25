using System.Collections.Generic;
using System.Security.Claims;
using NSubstitute;
using NUnit.Framework;
using PlexSSO.Model;
using PlexSSO.Model.Internal;
using PlexSSO.Oidc.Plugin.Model;
using PlexSSO.Oidc.Plugin.Service;
using PlexSSO.Service.Config;

namespace PlexSSO.Test.Service;

public class OidcServiceTest
{
    private OidcConfig _oidcConfig;
    private IConfigurationService _configurationService;
    private Identity _identity;

    [SetUp]
    public void Setup()
    {
        _oidcConfig = new OidcConfig();

        var plexSsoConfig = new PlexSsoConfig { CookieDomain = ".example.com" };

        _configurationService = Substitute.For<IConfigurationService>();
        _configurationService.Config.Returns(plexSsoConfig);
        _configurationService.GetPluginConfig<OidcConfig>(OidcConstants.PluginName).Returns(_oidcConfig);

        _identity = new Identity(new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "victim-uuid-0001"),
            new Claim(Constants.AccessTierClaim, "NormalUser"),
            new Claim(Constants.AccessTokenClaim, "plex-access-token"),
            new Claim(Constants.ServerIdentifierClaim, "server-id"),
            new Claim(Constants.UsernameClaim, "victim"),
            new Claim(Constants.DisplayNameClaim, "Victim User"),
            new Claim(Constants.EmailClaim, "victim@example.invalid"),
        });
    }

    private OidcService CreateService() => new(_configurationService);

    // --- Client registry: exact-match only, closed by default -------------------------------

    [Test]
    public void IsRedirectUriAllowed_EmptyRegistry_RejectsEverything()
    {
        var service = CreateService();

        Assert.That(service.IsRedirectUriAllowed("jellyfin", "https://jellyfin.example.com/cb"), Is.False);
    }

    [Test]
    public void IsRedirectUriAllowed_RegisteredClientExactMatch_IsAllowed()
    {
        _oidcConfig.Clients.Add(new OidcClient
        {
            ClientId = "jellyfin",
            RedirectUris = new[] { "https://jellyfin.example.com/cb" }
        });
        var service = CreateService();

        Assert.That(service.IsRedirectUriAllowed("jellyfin", "https://jellyfin.example.com/cb"), Is.True);
    }

    [Test]
    public void IsRedirectUriAllowed_UnregisteredClientId_IsRejected()
    {
        _oidcConfig.Clients.Add(new OidcClient
        {
            ClientId = "jellyfin",
            RedirectUris = new[] { "https://jellyfin.example.com/cb" }
        });
        var service = CreateService();

        Assert.That(service.IsRedirectUriAllowed("jellyfin-evil", "https://attacker.example/cb"), Is.False);
    }

    [Test]
    public void IsRedirectUriAllowed_RegisteredClientDifferentRedirectUri_IsRejected()
    {
        _oidcConfig.Clients.Add(new OidcClient
        {
            ClientId = "jellyfin",
            RedirectUris = new[] { "https://jellyfin.example.com/cb" }
        });
        var service = CreateService();

        // Same registered client_id, but an unregistered redirect_uri (e.g. an attacker's origin).
        Assert.That(service.IsRedirectUriAllowed("jellyfin", "https://attacker.example/cb"), Is.False);
    }

    [Test]
    public void IsRedirectUriAllowed_SubstringLookalikeHost_IsNotTreatedAsAMatch()
    {
        _oidcConfig.Clients.Add(new OidcClient
        {
            ClientId = "jellyfin",
            RedirectUris = new[] { "https://jellyfin.example.com/cb" }
        });
        var service = CreateService();

        // "jellyfin.example.com.attacker.com" is a different, unregistered origin - must not be
        // accepted just because it contains the registered host as a substring/prefix.
        Assert.That(
            service.IsRedirectUriAllowed("jellyfin", "https://jellyfin.example.com.attacker.com/cb"),
            Is.False);
    }

    // --- Opt-in cookie-domain trust mode -----------------------------------------------------

    [Test]
    public void IsRedirectUriAllowed_TrustCookieDomainDisabled_UnregisteredSameDomainUriIsStillRejected()
    {
        _oidcConfig.TrustCookieDomainClients = false;
        var service = CreateService();

        Assert.That(service.IsRedirectUriAllowed("jellyfin", "https://jellyfin.example.com/cb"), Is.False);
    }

    [Test]
    public void IsRedirectUriAllowed_TrustCookieDomainEnabled_SubdomainOfCookieDomainIsAllowed()
    {
        _oidcConfig.TrustCookieDomainClients = true;
        var service = CreateService();

        Assert.That(service.IsRedirectUriAllowed("anything", "https://jellyfin.example.com/cb"), Is.True);
    }

    [Test]
    public void IsRedirectUriAllowed_TrustCookieDomainEnabled_BareCookieDomainIsAllowed()
    {
        _oidcConfig.TrustCookieDomainClients = true;
        var service = CreateService();

        // cookieDomain = ".example.com" should also match the bare "example.com" host itself.
        Assert.That(service.IsRedirectUriAllowed("anything", "https://example.com/cb"), Is.True);
    }

    [Test]
    public void IsRedirectUriAllowed_TrustCookieDomainEnabled_UnrelatedHostIsStillRejected()
    {
        _oidcConfig.TrustCookieDomainClients = true;
        var service = CreateService();

        Assert.That(service.IsRedirectUriAllowed("anything", "https://attacker.example/cb"), Is.False);
    }

    [Test]
    public void IsRedirectUriAllowed_TrustCookieDomainEnabled_LookalikeSuffixHostIsStillRejected()
    {
        _oidcConfig.TrustCookieDomainClients = true;
        var service = CreateService();

        // "notexample.com" ends with "example.com" as a raw string but is not a subdomain of it.
        Assert.That(service.IsRedirectUriAllowed("anything", "https://notexample.com/cb"), Is.False);
    }

    // --- CreateAuthorizationCode / ExchangeCodeForToken must honour the same validation --------

    [Test]
    public void CreateAuthorizationCode_UnregisteredClient_ReturnsNull()
    {
        var service = CreateService();

        var code = service.CreateAuthorizationCode(_identity, "jellyfin-evil", "https://attacker.example/cb", "nonce");

        Assert.That(code, Is.Null);
    }

    [Test]
    public void CreateAuthorizationCode_RegisteredClient_ReturnsCodeExchangeableForToken()
    {
        _oidcConfig.Clients.Add(new OidcClient
        {
            ClientId = "jellyfin",
            RedirectUris = new[] { "https://jellyfin.example.com/cb" }
        });
        var service = CreateService();

        var code = service.CreateAuthorizationCode(_identity, "jellyfin", "https://jellyfin.example.com/cb", "nonce123");
        Assert.That(code, Is.Not.Null);

        var token = service.ExchangeCodeForToken(
            "http://127.0.0.1:4200", code, "jellyfin", "https://jellyfin.example.com/cb");

        Assert.That(token, Is.Not.Null);
        Assert.That(token.AccessToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ExchangeCodeForToken_MismatchedRedirectUriAtExchangeTime_IsRejected()
    {
        _oidcConfig.Clients.Add(new OidcClient
        {
            ClientId = "jellyfin",
            RedirectUris = new[] { "https://jellyfin.example.com/cb" }
        });
        var service = CreateService();

        var code = service.CreateAuthorizationCode(_identity, "jellyfin", "https://jellyfin.example.com/cb", "nonce123");
        Assert.That(code, Is.Not.Null);

        // Even a legitimately-issued code must not redeem against a different redirect_uri than the
        // one it was issued for.
        var token = service.ExchangeCodeForToken(
            "http://127.0.0.1:4200", code, "jellyfin", "https://attacker.example/cb");

        Assert.That(token, Is.Null);
    }

    [Test]
    public void ExchangeCodeForToken_UnknownCode_IsRejected()
    {
        var service = CreateService();

        var token = service.ExchangeCodeForToken(
            "http://127.0.0.1:4200", "not-a-real-code", "jellyfin", "https://jellyfin.example.com/cb");

        Assert.That(token, Is.Null);
    }
}
