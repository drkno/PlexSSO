namespace PlexSSO.Oidc.Plugin.Model;

public class OidcConfig
{
    public bool Enabled { get; set; } = true;
    public int AccessTokenLifetimeSeconds { get; set; } = 3600;
    public int AuthorizationCodeLifetimeSeconds { get; set; } = 600;
    public string SigningKey { get; set; }
    public int SigningKeySizeBits { get; set; } = 2048;
}