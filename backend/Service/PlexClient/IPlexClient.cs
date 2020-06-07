using System.Threading.Tasks;

namespace PlexSSO.Service.PlexClient
{
    public interface IPlexClient
    {
        ServerIdentifier GetLocalServerIdentifier(string path = "Preferences.xml");
        Task<AccessTier> GetAccessTier(ServerIdentifier serverId, Token token);
        Task<User> GetUserInfo(Token token);
    }
}

