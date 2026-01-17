using PlexSSO.Model.Types;

namespace PlexSSO.Service.PlexClient
{
    public class User
    {
        public UserIdentifier UserIdentifier { get; }
        public Username Username { get; }
        public DisplayName DisplayName { get; }
        public Email Email { get; }
        public Thumbnail Thumbnail { get; }

        public User(string username, string displayName, string email, string thumbnail, string uuid)
        {
            Username = new Username(username, email);
            DisplayName = new DisplayName(displayName, Username);
            Email = new Email(email);
            Thumbnail = new Thumbnail(thumbnail);
            UserIdentifier = new UserIdentifier(uuid);
        }
    }
}
