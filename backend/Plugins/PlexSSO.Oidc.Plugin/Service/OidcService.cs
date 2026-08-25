using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using PlexSSO.Model.Internal;
using PlexSSO.Oidc.Plugin.Model;
using PlexSSO.Service.Config;

namespace PlexSSO.Oidc.Plugin.Service;

public class OidcService : IOidcService
{
    private readonly OidcConfig _config;
    private readonly string _cookieDomain;
    private readonly RsaSecurityKey _signingKey;
        
    private readonly IMemoryCache _authCodes;

    public OidcService(IConfigurationService configService)
    {
        _config = configService
            .GetPluginConfig<OidcConfig>(OidcConstants.PluginName);
        _cookieDomain = configService.Config?.CookieDomain;

        if (_config.TrustCookieDomainClients)
        {
            Console.WriteLine(
                "WARNING: oidc plugin has trustCookieDomainClients enabled - any application " +
                $"reachable under cookieDomain ({_cookieDomain}) can receive OIDC authorization " +
                "codes and be issued tokens for any client_id it requests, without being listed " +
                "in the oidc \"clients\" registry. Only leave this enabled if every service on " +
                "that domain is fully trusted. See docs/oidc-plugin.md.");
        }
            
        var rsa = RSA.Create(_config.SigningKeySizeBits);
        if (!string.IsNullOrWhiteSpace(_config.SigningKey))
        {
            rsa.FromXmlString(_config.SigningKey);
        }
        else
        {
            var xml = rsa.ToXmlString(true);
            _config.SigningKey = xml;
            configService.SavePluginConfig(OidcConstants.PluginName, _config);
        }
        _signingKey = new RsaSecurityKey(rsa) { KeyId = "plexsso-signing-key" };
        
        _authCodes = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromMinutes(_config.AuthorizationCodeLifetimeSeconds)
        });
    }

    public JsonWebKeySet GetJsonWebPublicKeySet()
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(_signingKey);
        return new JsonWebKeySet { Keys = { jwk } };
    }

    public SecurityKey GetSigningKey()
    {
        return _signingKey;
    }

    public bool IsRedirectUriAllowed(string clientId, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
        {
            return false;
        }

        // Exact, case-sensitive match against the registered client/redirect-uri registry only.
        // Never prefix/substring match - "https://good.example.com.evil.com" must not match
        // "https://good.example.com".
        var registeredMatch = _config.Clients != null && _config.Clients.Any(client =>
            string.Equals(client.ClientId, clientId, StringComparison.Ordinal) &&
            client.RedirectUris != null &&
            client.RedirectUris.Any(uri => string.Equals(uri, redirectUri, StringComparison.Ordinal)));
        if (registeredMatch)
        {
            return true;
        }

        return _config.TrustCookieDomainClients && IsWithinCookieDomain(redirectUri);
    }

    private bool IsWithinCookieDomain(string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(_cookieDomain))
        {
            return false;
        }
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return false;
        }
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        var domain = _cookieDomain.Trim();
        if (domain.StartsWith('.'))
        {
            // Leading-dot cookie domain convention (see PlexSsoConfig.CookieDomain): matches the
            // bare domain itself and any subdomain of it.
            var bareDomain = domain[1..];
            return string.Equals(uri.Host, bareDomain, StringComparison.OrdinalIgnoreCase)
                   || uri.Host.EndsWith("." + bareDomain, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(uri.Host, domain, StringComparison.OrdinalIgnoreCase);
    }

    public string CreateAuthorizationCode(Identity identity, string clientId, string redirectUri, string nonce)
    {
        if (!IsRedirectUriAllowed(clientId, redirectUri))
        {
            return null;
        }

        var code = Guid.NewGuid().ToString("N");
        var item = new OidcAuthCodeItem
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            Identity = identity,
            Nonce = nonce,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_config.AuthorizationCodeLifetimeSeconds)
        };

        _authCodes.Set(code, item, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.AuthorizationCodeLifetimeSeconds)
        });
        return code;
    }

    public OidcTokenResponse ExchangeCodeForToken(
        string issuer,
        string code,
        string clientId,
        string redirectUri)
    {
        if (!_authCodes.TryGetValue(code, out OidcAuthCodeItem item))
        {
            return null;
        }
        _authCodes.Remove(code);

        if (item.ExpiresAt < DateTime.UtcNow || item.ClientId != clientId || item.RedirectUri != redirectUri)
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var now = DateTime.UtcNow;

        var claims = new List<Claim>(item.Identity.AsClaims())
        {
            new Claim(JwtRegisteredClaimNames.Sub, item.Identity.UserIdentifier.Value ?? ""),
            new Claim(JwtRegisteredClaimNames.PreferredUsername, item.Identity.Username.Value ?? ""),
            new Claim(JwtRegisteredClaimNames.Name, item.Identity.DisplayName.Value ?? item.Identity.Username.Value ?? ""),
            new Claim(JwtRegisteredClaimNames.Iss, issuer),
            new Claim(JwtRegisteredClaimNames.Aud, clientId),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(now.AddSeconds(_config.AccessTokenLifetimeSeconds)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (!string.IsNullOrEmpty(item.Nonce))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, item.Nonce));
        }
        if (!string.IsNullOrEmpty(item.Identity.Email?.Value))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, item.Identity.Email.Value));
        }
        if (!string.IsNullOrEmpty(item.Identity.Thumbnail?.Value))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, item.Identity.Thumbnail.Value));
        }

        var idTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddSeconds(_config.AccessTokenLifetimeSeconds),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        };

        var idToken = tokenHandler.CreateToken(idTokenDescriptor);
        var idTokenString = tokenHandler.WriteToken(idToken);

        return new OidcTokenResponse
        {
            AccessToken = idTokenString, // Reusing ID token as access token for simplicity
            IdToken = idTokenString,
            ExpiresIn = _config.AccessTokenLifetimeSeconds,
            TokenType = "Bearer"
        };
    }
}