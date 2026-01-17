using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
    private readonly RsaSecurityKey _signingKey;
        
    private readonly IMemoryCache _authCodes;

    public OidcService(IConfigurationService configService)
    {
        _config = configService
            .GetPluginConfig<OidcConfig>(OidcConstants.PluginName);
            
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

    public string CreateAuthorizationCode(Identity identity, string clientId, string redirectUri, string nonce)
    {
        var code = Guid.NewGuid().ToString("N");
        var item = new OidcAuthCodeItem
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            Subject = identity.UserIdentifier,
            Username = identity.Username,
            DisplayName = identity.DisplayName,
            Email = identity.Email,
            Picture = identity.Thumbnail,
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
        Identity identity,
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

        // Create ID Token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, item.Subject?.Value ?? ""),
            new Claim(JwtRegisteredClaimNames.PreferredUsername, item.Username?.Value ?? ""),
            new Claim(JwtRegisteredClaimNames.Name, item.DisplayName?.Value ?? item.Username?.Value ?? ""),
            new Claim(JwtRegisteredClaimNames.Iss, issuer),
            new Claim(JwtRegisteredClaimNames.Aud, clientId),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(now.AddSeconds(_config.AccessTokenLifetimeSeconds)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (!string.IsNullOrEmpty(item.Nonce))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, item.Nonce));
        }
        if (!string.IsNullOrEmpty(item.Email?.Value))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, item.Email.Value));
        }
        if (!string.IsNullOrEmpty(item.Picture?.Value))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, item.Picture.Value));
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