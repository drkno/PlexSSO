using PlexSSO.Service.PlexClient;

namespace PlexSSO.Model
{
    public class SsoResponse : BasicResponse
    {
        public AccessTier Tier { get; }
        public bool LoggedIn { get; }
        public bool AccessBlocked { get; }

        public SsoResponse(bool success, bool loggedIn, bool blocked, AccessTier accessTier) : base(success)
        {
            LoggedIn = loggedIn;
            AccessBlocked = blocked;
            Tier = accessTier;
        }
    }
}
