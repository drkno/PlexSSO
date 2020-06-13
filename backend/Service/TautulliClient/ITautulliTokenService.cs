using System.Threading.Tasks;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.TautulliClient
{
    public interface ITautulliTokenService
    {
        Task<TautulliToken> GetTautulliToken(PlexToken plexToken);
    }
}
