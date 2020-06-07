using PlexSSO.Model;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.Auth
{
    public interface IAuthValidator
    {
        SsoResponse ValidateAuthenticationStatus(
            AccessTier accessTier,
            bool loggedIn,
            string serviceName,
            string serviceUri,
            string userName
        );
    }
}
