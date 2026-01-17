namespace PlexSSO.Oidc.Plugin.Model;

using System.Text.Json.Serialization;

public class OidcTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("id_token")]
    public string IdToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";
}