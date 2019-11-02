using PlexSSO.Service.PlexClient;

namespace PlexSSO.Model
{
    public class SsoResponse : BasicResponse
    {
        public AccessTier Tier { get; }

        public SsoResponse(bool success, AccessTier accessTier) : base(success)
        {
            Tier = accessTier;
        }
    }
}
