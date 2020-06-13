namespace PlexSSO.Model
{
    public static class Constants
    {
        public const string AccessTierClaim = "AccessTier";
        public const string AccessTokenClaim = "AccessToken";
        public const string ServerIdentifierClaim = "ServerIdentifier";
        public const string UsernameClaim = "Username";
        public const string EmailClaim = "Email";
        public const string ThumbnailClaim = "Thumbnail";
        public const string SsoServiceHeader = "X-PlexSSO-For";
        public const string SsoOrigionalUriHeader = "X-PlexSSO-Original-URI";
        public const string UpstreamProtocolHeader = "X-Upstream-Protocol";
        public const string ForwardedProtoHeader = "X-Forwarded-Proto";
        public const string FrontEndHttpsHeader = "Front-End-Https";
        public const string ForwardedProtocolHeader = "X-Forwarded-Protocol";
        public const string ForwardedSslHeader = "X-Forwarded-Ssl";
        public const string UrlSchemeHeader = "X-Url-Scheme";
    }
}
