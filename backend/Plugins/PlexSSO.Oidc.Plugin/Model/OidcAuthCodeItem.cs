using System;
using PlexSSO.Model.Types;

namespace PlexSSO.Oidc.Plugin.Model;

public class OidcAuthCodeItem
{
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public UserIdentifier Subject { get; set; }
    public string Nonce { get; set; }
    public Username Username { get; set; }
    public DisplayName DisplayName { get; set; }
    public Email Email { get; set; }
    public Thumbnail Picture { get; set; }
    public DateTime ExpiresAt { get; set; }
}