using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using PlexSSO.Model;
using PlexSSO.Oidc.Plugin.Controllers;
using PlexSSO.Oidc.Plugin.Model;
using PlexSSO.Oidc.Plugin.Service;
using PlexSSO.Service.Config;

namespace PlexSSO.Test.Controllers;

public class OidcControllerTest
{
    private IOidcService _oidcService;
    private IConfigurationService _configurationService;
    private OidcConfig _oidcConfig;

    [SetUp]
    public void Setup()
    {
        _oidcService = Substitute.For<IOidcService>();

        _oidcConfig = new OidcConfig { Enabled = true };
        _configurationService = Substitute.For<IConfigurationService>();
        _configurationService.GetPluginConfig<OidcConfig>(OidcConstants.PluginName).Returns(_oidcConfig);
    }

    private OidcController CreateController(bool authenticated)
    {
        var controller = new OidcController(_oidcService, _configurationService);

        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new Claim(Constants.UsernameClaim, "victim"),
            new Claim(Constants.EmailClaim, "victim@example.invalid"),
        };
        if (authenticated)
        {
            claims.Add(new Claim(Constants.AccessTokenClaim, "plex-token"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    [Test]
    public void Authorize_NotAuthenticated_RedirectsToLogin_AndNeverCreatesACode()
    {
        var controller = CreateController(authenticated: false);

        var result = controller.Authorize("code", "jellyfin-evil", "https://attacker.example/cb", "state1", "nonce1");

        Assert.That(result, Is.InstanceOf<RedirectResult>());
        Assert.That(((RedirectResult)result).Url, Does.StartWith("/sso/login"));
        _oidcService.DidNotReceive().CreateAuthorizationCode(
            Arg.Any<PlexSSO.Model.Internal.Identity>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public void Authorize_UnsupportedResponseType_ReturnsBadRequest()
    {
        var controller = CreateController(authenticated: true);

        var result = controller.Authorize("token", "jellyfin", "https://jellyfin.example.com/cb", "state1", "nonce1");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Authorize_PluginDisabled_ReturnsNotFound()
    {
        _oidcConfig.Enabled = false;
        var controller = CreateController(authenticated: true);

        var result = controller.Authorize("code", "jellyfin", "https://jellyfin.example.com/cb", "state1", "nonce1");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void Authorize_UnregisteredClientRejectedByService_ReturnsBadRequest_AndNeverRedirectsToAttackerUri()
    {
        // When the OIDC service rejects the client_id/redirect_uri pair (unregistered client), the controller must
        // return an error response directly and must NEVER issue a redirect to the caller-supplied redirect_uri - doing
        // so is exactly what let an attacker exfiltrate a victim's authorization code.
        _oidcService.CreateAuthorizationCode(
                Arg.Any<PlexSSO.Model.Internal.Identity>(), "jellyfin-evil", "https://attacker.example/cb", "nonce1")
            .Returns((string)null);

        var controller = CreateController(authenticated: true);

        var result = controller.Authorize(
            "code", "jellyfin-evil", "https://attacker.example/cb", "state1", "nonce1");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(result, Is.Not.InstanceOf<RedirectResult>());
    }

    [Test]
    public void Authorize_RegisteredClientAllowedByService_RedirectsWithCodeAndState()
    {
        _oidcService.CreateAuthorizationCode(
                Arg.Any<PlexSSO.Model.Internal.Identity>(), "jellyfin", "https://jellyfin.example.com/cb", "nonce1")
            .Returns("abc123");

        var controller = CreateController(authenticated: true);

        var result = controller.Authorize(
            "code", "jellyfin", "https://jellyfin.example.com/cb", "state1", "nonce1");

        Assert.That(result, Is.InstanceOf<RedirectResult>());
        var redirectUrl = ((RedirectResult)result).Url;
        Assert.That(redirectUrl, Does.StartWith("https://jellyfin.example.com/cb"));
        Assert.That(redirectUrl, Does.Contain("code=abc123"));
        Assert.That(redirectUrl, Does.Contain("state=state1"));
    }

    // --- /oidc/token ---------------------------------------------------------------------------

    [Test]
    public void Token_UnsupportedGrantType_ReturnsBadRequest()
    {
        var controller = CreateController(authenticated: false);

        var result = controller.Token("client_credentials", "code123", "https://jellyfin.example.com/cb", "jellyfin");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Token_PluginDisabled_ReturnsNotFound()
    {
        _oidcConfig.Enabled = false;
        var controller = CreateController(authenticated: false);

        var result = controller.Token("authorization_code", "code123", "https://jellyfin.example.com/cb", "jellyfin");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void Token_ServiceRejectsExchange_ReturnsBadRequest()
    {
        _oidcService.ExchangeCodeForToken(Arg.Any<string>(), "bad-code", "jellyfin", "https://jellyfin.example.com/cb")
            .Returns((OidcTokenResponse)null);
        var controller = CreateController(authenticated: false);

        var result = controller.Token("authorization_code", "bad-code", "https://jellyfin.example.com/cb", "jellyfin");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Token_ServiceReturnsToken_ReturnsOkWithToken()
    {
        var tokenResponse = new OidcTokenResponse
        {
            AccessToken = "the-jwt",
            IdToken = "the-jwt",
            ExpiresIn = 3600,
            TokenType = "Bearer"
        };
        _oidcService.ExchangeCodeForToken(Arg.Any<string>(), "good-code", "jellyfin", "https://jellyfin.example.com/cb")
            .Returns(tokenResponse);
        var controller = CreateController(authenticated: false);

        var result = controller.Token("authorization_code", "good-code", "https://jellyfin.example.com/cb", "jellyfin");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result).Value, Is.SameAs(tokenResponse));
    }
}