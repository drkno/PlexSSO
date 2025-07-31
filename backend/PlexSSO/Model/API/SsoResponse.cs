using System;
using PlexSSO.Model.Types;

namespace PlexSSO.Model.API
{
    public class SsoResponse : BasicResponse
    {
        public AccessTier Tier { get; }
        public bool LoggedIn { get; }
        public bool AccessBlocked { get; }
        public int Status { get; }
        public string Message { get; }

        public SsoResponse(bool success, bool loggedIn, bool blocked, AccessTier accessTier, int status,
            string message) : base(success)
        {
            LoggedIn = loggedIn;
            AccessBlocked = blocked;
            Tier = accessTier;
            Status = status;
            Message = message;
        }

        private bool Equals(SsoResponse other)
        {
            return base.Equals(other) && Tier == other.Tier && LoggedIn == other.LoggedIn &&
                   AccessBlocked == other.AccessBlocked && Status == other.Status && Message == other.Message;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SsoResponse)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), (int)Tier, LoggedIn, AccessBlocked, Status, Message);
        }
    }
}