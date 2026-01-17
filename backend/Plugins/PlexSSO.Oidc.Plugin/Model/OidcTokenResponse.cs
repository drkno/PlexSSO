namespace PlexSSO.Oidc.Plugin.Model;

public class OidcTokenResponse
{
    public string AccessToken { get; set; }
    public string IdToken { get; set; }
    public int? ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}