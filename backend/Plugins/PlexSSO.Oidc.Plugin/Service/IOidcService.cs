using Microsoft.IdentityModel.Tokens;
using PlexSSO.Model.Internal;
using PlexSSO.Oidc.Plugin.Model;

namespace PlexSSO.Oidc.Plugin.Service;

public interface IOidcService
{
    JsonWebKeySet GetJsonWebPublicKeySet();
    SecurityKey GetSigningKey();

    /// <summary>
    /// Returns true only if clientId/redirectUri is an exact match for a registered
    /// <see cref="Model.OidcClient"/>, or (when opted in) redirectUri's host falls under the
    /// server's cookie domain. Used to decide whether /oidc/authorize may issue a code and redirect
    /// - callers must never redirect to redirectUri when this returns false.
    /// </summary>
    bool IsRedirectUriAllowed(string clientId, string redirectUri);

    /// <summary>
    /// Creates and returns a new authorization code for identity, or null if clientId/redirectUri is
    /// not permitted (see <see cref="IsRedirectUriAllowed"/>). Callers must not redirect the user
    /// anywhere on a null result.
    /// </summary>
    string CreateAuthorizationCode(Identity identity, string clientId, string redirectUri, string nonce);
    OidcTokenResponse ExchangeCodeForToken(string issuer, string code, string clientId, string redirectUri);
}