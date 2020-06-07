namespace PlexSSO.Service.PlexClient
{
    public class User
    {
        public string Username { get; }
        public string Email { get; }
        public string Thumbnail { get; }

        public User(string username, string email, string thumbnail)
        {
            Username = string.IsNullOrWhiteSpace(username) ? email : username;
            Email = email;
            Thumbnail = thumbnail;
        }
    }
}
