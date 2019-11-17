using PlexSSO.Service.PlexClient;

namespace PlexSSO.Model
{
    public class SsoResponse : BasicResponse
    {
        public AccessTier Tier { get; }
        public bool LoggedIn { get; }

        public SsoResponse(bool success, bool loggedIn, AccessTier accessTier) : base(success)
        {
            LoggedIn = loggedIn;
            Tier = accessTier;
        }
    }
}
