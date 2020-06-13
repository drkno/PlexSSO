using System.Threading.Tasks;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.OmbiClient
{
    public interface IOmbiTokenService
    {
        Task<OmbiToken> GetOmbiToken(PlexToken plexToken);
    }
}
