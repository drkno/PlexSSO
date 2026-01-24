using System;
using PlexSSO.Model.Internal;

namespace PlexSSO.Oidc.Plugin.Model;

public class OidcAuthCodeItem
{
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public string Nonce { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Identity Identity { get; set; }
}