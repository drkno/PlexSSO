using System.Linq;
using PlexSSO.Model;
using PlexSSO.Service.Config;
using PlexSSO.Service.PlexClient;
using static PlexSSO.Service.Config.PlexSsoConfig;

namespace PlexSSO.Service.Auth
{
    public class AuthenticationValidator : IAuthValidator
    {
        private readonly IConfigurationService _configurationService;

        public AuthenticationValidator(IConfigurationService configurationService)
        {
            this._configurationService = configurationService;
        }

        public SsoResponse ValidateAuthenticationStatus(
            AccessTier accessTier,
            bool loggedIn,
            string serviceName,
            string serviceUri,
            string userName
        )
        {
            var rules = _configurationService.GetAccessControls(serviceName);

            foreach (var rule in rules)
            {
                if (rule.Path != null && !serviceUri.StartsWith(rule.Path))
                {
                    continue;
                }

                var block = rule.ControlType == ControlType.Allow ?
                    accessTier.IsHigherTierThan(rule.MinimumAccessTier ?? AccessTier.Failure) :
                    accessTier.IsLowerTierThan(rule.MinimumAccessTier ?? AccessTier.NoAccess);
                
                if (rule.Exempt.Contains(userName))
                {
                    block = !block;
                }

                if (block) {
                    return new SsoResponse(
                        true,
                        loggedIn,
                        true,
                        AccessTier.NoAccess
                    );
                }
            }

            return new SsoResponse(
                true,
                loggedIn,
                rules.Length == 0 ? accessTier == AccessTier.NoAccess : false,
                accessTier
            );
        }
    }

    public static class AccessTierExtensions
    {
        public static bool IsLowerTierThan(this AccessTier currentAccess, AccessTier accessTier)
        {
            return currentAccess > accessTier;
        }
        
        public static bool IsHigherTierThan(this AccessTier currentAccess, AccessTier accessTier)
        {
            return currentAccess < accessTier;
        }
    }
}
