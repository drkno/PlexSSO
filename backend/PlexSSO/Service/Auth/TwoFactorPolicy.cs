using System.Linq;
using System.Threading.Tasks;
using PlexSSO.Model.Types;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.Auth;

/// <summary>
/// Applies the opt-in RequireTwoFactorAuthentication setting.
/// </summary>
public class TwoFactorPolicy(Config.IConfigurationService configurationService, IPlexClient plexClient)
{
    public string RequiredMessage => configurationService.Config.TwoFactorRequiredMessage;

    public bool AppliesTo(Username username)
    {
        var config = configurationService.Config;
        return config.RequireTwoFactorAuthentication
               && !(config.TwoFactorExempt ?? []).Contains(username);
    }

    /// <returns>
    /// True if the user may proceed, false if 2FA is required but disabled on their account,
    /// or null if it is required and plex.tv could not tell us.
    /// </returns>
    public async Task<bool?> IsSatisfied(Username username, AccessToken token)
    {
        if (!AppliesTo(username))
        {
            return true;
        }
        return await plexClient.IsTwoFactorEnabled(token);
    }
}
