using System.Linq;
using Microsoft.Extensions.Logging;
using PlexSSO.Extensions;
using PlexSSO.Model.API;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using static PlexSSO.Model.Internal.PlexSsoConfig;

namespace PlexSSO.Service.Auth
{
    public class AuthenticationValidator : IAuthValidator
    {
        private readonly Config.IConfigurationService<PlexSsoConfig> _configurationService;

        public AuthenticationValidator(Config.IConfigurationService<PlexSsoConfig> configurationService,
                                       ILogger<AuthenticationValidator> logger)
        {
            _configurationService = configurationService;
        }

        public SsoResponse ValidateAuthenticationStatus(
            Identity identity,
            ServiceName serviceName,
            ServiceUri serviceUri
        )
        {
            var (controlledByRules, matchingAccessControl) = GetFirstMatchingAccessControl(serviceName, serviceUri, identity);
            if (matchingAccessControl != null)
            {
                var blockMessage = string.IsNullOrWhiteSpace(matchingAccessControl.BlockMessage)
                    ? _configurationService.Config.DefaultAccessDeniedMessage
                    : matchingAccessControl.BlockMessage;

                return new SsoResponse(
                    true,
                    identity.IsAuthenticated,
                    true,
                    AccessTier.NoAccess,
                    403,
                    blockMessage
                );
            }
            else
            {
                var message = "";
                var status = 200;
                if (!controlledByRules && identity.AccessTier == AccessTier.NoAccess)
                {
                    if (identity.IsAuthenticated)
                    {
                        status = 403;
                        message = _configurationService.Config.DefaultAccessDeniedMessage;
                    }
                    else
                    {
                        status = 401;
                        message = "Login Required";
                    }
                }

                return new SsoResponse(
                    true,
                    identity.IsAuthenticated,
                    status != 200,
                    identity.AccessTier,
                    status,
                    message
                );
            }
        }

        private (bool, AccessControl) GetFirstMatchingAccessControl(ServiceName serviceName,
                                                            ServiceUri serviceUri,
                                                            Identity identity)
        {
            var controls = GetAccessControls(serviceName);
            var matching = controls
                .Where(control => control.Path == null || serviceUri.Value.StartsWith(control.Path))
                .FirstOrDefault(control =>
                {
                    var block = control.ControlType == ControlType.Allow
                        ? identity.AccessTier.IsHigherTierThan(control.MinimumAccessTier ?? AccessTier.Failure)
                        : identity.AccessTier.IsLowerTierThan(control.MinimumAccessTier ?? AccessTier.NoAccess);
                    if (control.Exempt.Contains(identity.Username))
                    {
                        block = !block;
                    }

                    return block;
                });
            return (controls.Length > 0, matching);
        }

        private AccessControl[] GetAccessControls(ServiceName serviceName)
        {
            var allAccessControls = _configurationService.Config.AccessControls;
            if (serviceName == null
                || (!allAccessControls.TryGetValue(serviceName.Value, out var accessControls)
                && !allAccessControls.TryGetValue("default", out accessControls)))
            {
                accessControls = new AccessControl[0];
            }
            return accessControls;
        }
    }
}
