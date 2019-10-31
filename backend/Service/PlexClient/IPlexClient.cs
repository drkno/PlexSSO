using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlexSSO.Service.PlexClient
{
    public interface IPlexClient
    {
        Task<IEnumerable<(string, string, string)>> GetServers(Token token);
        IEnumerable<(ServerIdentifier, AccessTier)> GetServerDetails(IEnumerable<(string, string, string)> servers);
        ServerIdentifier GetLocalServerIdentifier(string path = "Preferences.xml");
        Task<AccessTier> GetAccessTier(ServerIdentifier serverId, Token token);
    }
}

