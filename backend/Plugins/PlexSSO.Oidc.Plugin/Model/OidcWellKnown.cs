using System.Text.Json.Serialization;

namespace PlexSSO.Oidc.Plugin.Model;

public class OidcWellKnown {
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; }
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; }
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; }
    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; }
    [JsonPropertyName("userinfo_endpoint")]
    public string UserinfoEndpoint { get; set; }
    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported { get; set; }
    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; }
    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; }
    [JsonPropertyName("subject_types_supported")]
    public string[] SubjectTypesSupported { get; set; }
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public string[] IdTokenSigningAlgValuesSupported { get; set; }
}