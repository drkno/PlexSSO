using System.Threading.Tasks;
using PlexSSO.Model.Types;

namespace PlexSSO.Service.PlexClient
{
    public interface IPlexClient
    {
        ServerIdentifier GetPlexServerIdentifier();
        Task<AccessTier> GetAccessTier(ServerIdentifier serverId, AccessToken token);
        Task<User> GetUserInfo(AccessToken token);
        /// <returns>Whether the account has 2FA enabled, or null if this could not be determined.</returns>
        Task<bool?> IsTwoFactorEnabled(AccessToken token);
    }
}

