using System;
using System.Collections.Generic;

namespace PlexSSO.Oidc.Plugin.Model;

/// <summary>
/// A relying party ("client") that is permitted to use this OIDC provider.
///
/// Both <see cref="ClientId"/> and every entry in <see cref="RedirectUris"/> are matched using an
/// exact, case-sensitive string comparison - never as a prefix or substring - to prevent bypasses
/// such as "https://trusted.example.com.attacker.com".
/// </summary>
public class OidcClient
{
    /// <summary>The client_id this relying party will send to /oidc/authorize and /oidc/token.</summary>
    public string ClientId { get; set; }

    /// <summary>
    /// The exact, complete redirect_uri value(s) this client is allowed to receive an authorization
    /// code at. A request is only honoured if its redirect_uri is byte-for-byte identical to one of
    /// these entries.
    /// </summary>
    public string[] RedirectUris { get; set; } = [];
}

public class OidcConfig
{
    public bool Enabled { get; set; } = true;
    public int AccessTokenLifetimeSeconds { get; set; } = 3600;
    public int AuthorizationCodeLifetimeSeconds { get; set; } = 600;
    public string SigningKey { get; set; }
    public int SigningKeySizeBits { get; set; } = 2048;

    /// <summary>
    /// The registry of relying parties permitted to use this OIDC provider. A request to
    /// /oidc/authorize is only honoured if its client_id/redirect_uri pair matches an entry here
    /// exactly (see <see cref="OidcClient"/>) - or, when <see cref="TrustCookieDomainClients"/> is
    /// enabled, if the redirect_uri's host falls under the server's cookie domain. There is no
    /// default entry: an empty registry (the default) rejects every /oidc/authorize request.
    /// </summary>
    public List<OidcClient> Clients { get; set; } = [];

    /// <summary>
    /// OPT-IN. When true, any redirect_uri whose host exactly matches - or is a subdomain of, if
    /// the server's cookieDomain starts with "." - the server's configured cookieDomain is allowed,
    /// even if it is not listed in <see cref="Clients"/>.
    ///
    /// WARNING: enabling this trusts every application/subdomain reachable under cookieDomain to
    /// receive PlexSSO-issued OIDC authorization codes and to be minted a token for any client_id it
    /// asks for. Only enable this if you fully control cookieDomain and every service hosted on it -
    /// e.g. there is no way for an untrusted user to run a page (a wiki, a URL shortener, a status
    /// page, an S3/object-storage bucket, etc.) anywhere under that domain. If in doubt, leave this
    /// disabled and register clients explicitly in <see cref="Clients"/> instead. See
    /// docs/oidc-plugin.md for a full explanation.
    /// </summary>
    public bool TrustCookieDomainClients { get; set; } = false;
}