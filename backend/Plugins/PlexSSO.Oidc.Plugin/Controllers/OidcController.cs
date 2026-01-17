using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlexSSO.Service.Config;
using Microsoft.IdentityModel.Tokens;
using PlexSSO.Controllers;
using PlexSSO.Model.Types;
using PlexSSO.Oidc.Plugin.Model;
using PlexSSO.Oidc.Plugin.Service;

namespace PlexSSO.Oidc.Plugin.Controllers;

[ApiController]
public class OidcController(
    IOidcService oidcService,
    IConfigurationService configurationService)
    : CommonAuthController
{

    [HttpGet("/.well-known/openid-configuration")]
    public OidcWellKnown GetConfiguration()
    {
        var issuer = GetIssuer();
        return new OidcWellKnown
        {
            Issuer = issuer,
            AuthorizationEndpoint = $"{issuer}/oidc/authorize",
            TokenEndpoint = $"{issuer}/oidc/token",
            JwksUri = $"{issuer}/oidc/jwks",
            UserinfoEndpoint = $"{issuer}/oidc/userinfo",
            ScopesSupported = ["openid", "profile", "email"],
            ResponseTypesSupported = ["code"],
            GrantTypesSupported = ["authorization_code"],
            SubjectTypesSupported = ["public"],
            IdTokenSigningAlgValuesSupported = ["RS256"]
        };
    }

    [HttpGet("/oidc/jwks")]
    public JsonWebKeySet GetJwks()
    {
        return oidcService.GetJsonWebPublicKeySet();
    }

    [HttpGet("/oidc/authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "state")] string state,
        [FromQuery(Name = "nonce")] string nonce)
    {
        if (!configurationService
                .GetPluginConfig<OidcConfig>(OidcConstants.PluginName)
                .Enabled)
        {
            return NotFound();
        }

        if (responseType != "code")
        {
            return BadRequest("Unsupported response_type");
        }

        if (!Identity.IsAuthenticated)
        {
            var returnUrl = Uri.EscapeDataString(Request.Path + Request.QueryString);
            return Redirect($"/sso/login?returnUrl={returnUrl}");
        }

        var code = oidcService.CreateAuthorizationCode(Identity, clientId, redirectUri, nonce);
        var separator = redirectUri.Contains('?') ? "&" : "?";
        var redirectUrl = $"{redirectUri}{separator}code={code}&state={state}";
        return Redirect(redirectUrl);
    }

    [HttpPost("/oidc/token")]
    public IActionResult Token(
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "code")] string code,
        [FromForm(Name = "redirect_uri")] string redirectUri,
        [FromForm(Name = "client_id")] string clientId)
    {
        if (!configurationService
                .GetPluginConfig<OidcConfig>(OidcConstants.PluginName)
                .Enabled)
        {
            return NotFound();
        }

        if (grantType != "authorization_code")
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        var response = oidcService.ExchangeCodeForToken(Identity, GetIssuer(), code, clientId, redirectUri);
        if (response == null)
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        return Ok(response);
    }

    [HttpGet("/oidc/userinfo")]
    [Authorize]
    public OidcUserInfo UserInfo()
    {
        return new OidcUserInfo
        {
            Subject = Identity.UserIdentifier,
            Name = Identity.DisplayName?.Value ?? Identity.Username.Value,
            PreferredUsername = Identity.Username,
            Email = Identity.Email,
            Picture = Identity.Thumbnail
        };
    }

    private string GetIssuer() => $"{(Protocol == Protocol.Https ? "https" : "http")}://{Request.Host}";
}