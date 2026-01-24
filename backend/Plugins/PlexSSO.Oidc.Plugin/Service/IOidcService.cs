using Microsoft.IdentityModel.Tokens;
using PlexSSO.Model.Internal;
using PlexSSO.Oidc.Plugin.Model;

namespace PlexSSO.Oidc.Plugin.Service;

public interface IOidcService
{
    JsonWebKeySet GetJsonWebPublicKeySet();
    SecurityKey GetSigningKey();
    string CreateAuthorizationCode(Identity identity, string clientId, string redirectUri, string nonce);
    OidcTokenResponse ExchangeCodeForToken(string issuer, string code, string clientId, string redirectUri);
}