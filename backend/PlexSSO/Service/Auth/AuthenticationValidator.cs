using System.Linq;
using Microsoft.Extensions.Logging;
using PlexSSO.Extensions;
using PlexSSO.Model;
using PlexSSO.Model.API;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using static PlexSSO.Model.Internal.PlexSsoConfig;

namespace PlexSSO.Service.Auth;

public class AuthenticationValidator(
    Config.IConfigurationService configurationService,
    ILogger<AuthenticationValidator> logger)
    : IAuthValidator
{
    public SsoResponse ValidateAuthenticationStatus(
        Identity identity,
        ServiceName serviceName,
        ServiceUri serviceUri
    )
    {
        if (!identity.IsAuthenticated || identity.AccessTier == AccessTier.Failure)
        {
            return new SsoResponse(
                success: true,
                loggedIn: false,
                blocked: true,
                AccessTier.NoAccess,
                status: 401,
                Constants.DefaultLoginRequiredMessage
            );
        }

        if (!string.IsNullOrWhiteSpace(serviceUri?.Value)
            && TryGetFirstMatchingAccessControl(serviceName, serviceUri, identity,
                out var matchingAccessControl))
        {
            logger.LogWarning("Blocked access to {serviceName} for user {user} by rule with path {path}",
                serviceName, identity.Username, matchingAccessControl.Path);

            var blockMessage = GetAccessBlockedMessage(matchingAccessControl);
            return new SsoResponse(
                true,
                identity.IsAuthenticated,
                true,
                AccessTier.NoAccess,
                status: 403,
                blockMessage
            );
        }

        if (identity.AccessTier == AccessTier.NoAccess)
        {
            logger.LogWarning("Blocked access for user {user}, they have no server access", identity.Username);
                
            var blockMessage = GetAccessBlockedMessage();
            return new SsoResponse(
                success: true,
                loggedIn: identity.IsAuthenticated,
                blocked: true,
                AccessTier.NoAccess,
                status: 403,
                blockMessage
            );
        }

        return new SsoResponse(
            success: true,
            loggedIn: identity.IsAuthenticated,
            blocked: false,
            identity.AccessTier,
            status: 200,
            Constants.DefaultAuthorizedMessage
        );
    }

    private bool TryGetFirstMatchingAccessControl(ServiceName serviceName,
                                                  ServiceUri serviceUri,
                                                  Identity identity,
                                                  out AccessControl accessControl)
    {
        var controls = GetAccessControls(serviceName);
        var matching = controls
            .Where(control => control.Path == null || (serviceUri?.Value.StartsWith(control.Path) ?? false))
            .FirstOrDefault(control =>
            {
                var block = control.ControlType == ControlType.Allow
                    ? identity.AccessTier.HasMoreAccessThan(control.MinimumAccessTier)
                    : identity.AccessTier.HasLessAccessThan(control.MinimumAccessTier);
                if (control.Exempt.Contains(identity.Username))
                {
                    block = !block;
                }

                return block;
            });
        accessControl = matching;
        return matching != null;
    }

    private AccessControl[] GetAccessControls(ServiceName serviceName)
    {
        var allAccessControls = configurationService.Config.AccessControls;
        if (serviceName == null || !allAccessControls.TryGetValue(serviceName.Value, out var accessControls))
        {
            accessControls = [];
        }

        return accessControls;
    }

    private string GetAccessBlockedMessage(AccessControl accessControl = null)
    {
        if (!string.IsNullOrWhiteSpace(accessControl?.BlockMessage))
        {
            return accessControl.BlockMessage;
        }

        if (!string.IsNullOrWhiteSpace(configurationService.Config.DefaultAccessDeniedMessage))
        {
            return configurationService.Config.DefaultAccessDeniedMessage;
        }

        return Constants.DefaultAccessDeniedMessage;
    }
}