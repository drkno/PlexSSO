using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.Auth;

/// <summary>
/// Re-checks the access tier stored in the auth cookie against plex.tv so that users who lose (or change)
/// access to the Plex server lose (or change) access here too, without waiting for the cookie to expire.
/// When two-factor authentication is required, users who disable it on their Plex account also lose access.
///
/// Results are cached per access token rather than written back to the cookie, as most validations happen
/// within nginx auth_request sub-requests whose Set-Cookie headers never reach the browser.
/// </summary>
public class AccessTierRevalidator(
    IPlexClient plexClient,
    TwoFactorPolicy twoFactorPolicy,
    IMemoryCache cache,
    ILogger<AccessTierRevalidator> logger)
    : CookieAuthenticationEvents
{
    private static readonly TimeSpan RevalidationInterval = TimeSpan.FromMinutes(Constants.AccessTierRevalidationMinutes);
    private static readonly TimeSpan FailureRetryInterval = TimeSpan.FromMinutes(Constants.AccessTierFailureRetryMinutes);

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var identity = new Identity(context.Principal?.Claims ?? []);
        if (!identity.IsAuthenticated)
        {
            return;
        }

        var currentTier = await GetCurrentAccessTier(identity);
        if (currentTier == AccessTier.Failure)
        {
            // plex.tv is unavailable, keep the existing tier rather than locking everyone out
            return;
        }

        if (currentTier == AccessTier.NoAccess)
        {
            logger.LogWarning("User {user} no longer has access to the Plex server, signing out", identity.Username);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        if (currentTier != identity.AccessTier)
        {
            logger.LogDebug("Access tier for {user} changed from {old} to {new}",
                identity.Username, identity.AccessTier, currentTier);
            identity.AccessTier = currentTier;
            context.ReplacePrincipal(new ClaimsPrincipal(
                new ClaimsIdentity(identity.AsClaims(), CookieAuthenticationDefaults.AuthenticationScheme)));
            context.ShouldRenew = true;
        }
    }

    private async Task<AccessTier> GetCurrentAccessTier(Identity identity)
    {
        var serverIdentifier = identity.ServerIdentifier ?? plexClient.GetPlexServerIdentifier();
        var cacheKey = GetCacheKey(identity.AccessToken, serverIdentifier);

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            var tier = await plexClient.GetAccessTier(serverIdentifier, identity.AccessToken);
            if (tier is not (AccessTier.Failure or AccessTier.NoAccess))
            {
                var twoFactorSatisfied = await twoFactorPolicy.IsSatisfied(identity.Username, identity.AccessToken);
                tier = twoFactorSatisfied switch
                {
                    false => AccessTier.NoAccess,
                    // Status unavailable: treat like plex.tv being down and keep the existing tier
                    null => AccessTier.Failure,
                    true => tier
                };
            }
            entry.AbsoluteExpirationRelativeToNow = tier == AccessTier.Failure
                ? FailureRetryInterval
                : RevalidationInterval;
            return tier;
        });
    }

    private static string GetCacheKey(AccessToken token, ServerIdentifier serverIdentifier)
    {
        // Avoid holding raw Plex tokens as cache keys
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Value));
        return $"{nameof(AccessTierRevalidator)}:{serverIdentifier?.Value}:{Convert.ToHexString(hash)}";
    }
}
