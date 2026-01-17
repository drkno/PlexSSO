using System.Text.Json.Serialization;
using PlexSSO.Model.Types;

namespace PlexSSO.Oidc.Plugin.Model;

public class OidcUserInfo
{
    [JsonPropertyName("sub")]
    public UserIdentifier Subject { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("preferred_username")]
    public Username PreferredUsername { get; set; }
    [JsonPropertyName("email")]
    public Email Email { get; set; }
    [JsonPropertyName("picture")]
    public Thumbnail Picture { get; set; }
}