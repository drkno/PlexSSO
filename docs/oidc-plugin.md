# OIDC Plugin

`PlexSSO.Oidc.Plugin` turns PlexSSO into an OpenID Connect (OIDC) identity provider, exposing
`/.well-known/openid-configuration`, `/oidc/authorize`, `/oidc/token`, `/oidc/userinfo` and
`/oidc/jwks`. It is enabled by default and loaded automatically alongside PlexSSO's other plugins.

Its configuration lives in `oidc.config.json` inside your PlexSSO config directory (created
automatically on first run, alongside the main `config.json`).

## Client registry (`clients`)

**Every relying party (client) that is allowed to use this OIDC provider must be listed
explicitly.** By default `clients` is empty, which means every `/oidc/authorize` request is
rejected until you add at least one entry.

```js
{
  "enabled": true,
  "clients": [
    {
      "clientId": "jellyfin",
      "redirectUris": [
        "https://jellyfin.example.com/sso/OID/redirect/jellyfin"
      ]
    }
  ],
  "trustCookieDomainClients": false
}
```

| Property | Description |
|----------|-------------|
| `clientId` | The exact `client_id` value the relying party will send. |
| `redirectUris` | The complete, exact `redirect_uri` value(s) this client may use. Matching is byte-for-byte - no prefixes, wildcards, or partial-path matches. If your relying party can redirect to more than one callback URL, list every one explicitly. |

A request to `/oidc/authorize` is only honoured — i.e. an authorization code is minted and the
browser is redirected back to the caller — if its `client_id`/`redirect_uri` pair is an **exact**
match for an entry above. Any other request is rejected with
`400 {"error":"unauthorized_client"}` and the browser is **not** redirected anywhere, so a
malicious `redirect_uri` can never be used to exfiltrate a code for the logged-in user.

The same registry is consulted again at `/oidc/token`, in addition to the existing check that the
`client_id`/`redirect_uri` submitted there match what was submitted to `/oidc/authorize`.

## Trusting your whole cookie domain instead (`trustCookieDomainClients`)

If you don't want to maintain an explicit client list — for example because every service you run
sits on subdomains of one domain you fully control — you can opt in to trusting that domain as a
whole:

```js
{
  "trustCookieDomainClients": true
}
```

When enabled, any `redirect_uri` whose host exactly matches, or is a subdomain of, PlexSSO's main
`cookieDomain` setting (see the root `config.json`, e.g. `.example.com`) is allowed, **even if it
is not listed in `clients`**. `clients` entries are still honoured and are still checked first.

> **⚠️ Warning — read before enabling.**
> This trusts **every application and every subdomain reachable under `cookieDomain`** to receive
> PlexSSO-issued OIDC authorization codes for whichever user opens the link, and to have a token
> minted for **any `client_id` it asks for** (the `aud` claim is not restricted). Only enable this
> if:
> - You control every service hosted on that domain and its subdomains, **and**
> - None of them let an untrusted party host arbitrary content there (a wiki, URL shortener,
>   status page, object-storage bucket, PR-preview environment, etc. would all be enough to defeat
>   this).
>
> If you are not certain both of those hold, leave this disabled and register clients explicitly
> in `clients` instead. PlexSSO logs a warning on startup whenever this setting is enabled.

## Other settings

| Property | Description |
|----------|-------------|
| `enabled` | Master on/off switch for the plugin's endpoints. Defaults to `true`. See also [Disabling plugins](../Readme.md#disabling-plugins) to prevent the plugin's routes from being registered at all. |
| `accessTokenLifetimeSeconds` | Lifetime of issued access/ID tokens, in seconds. Default `3600`. |
| `authorizationCodeLifetimeSeconds` | Lifetime of an authorization code before it expires (and before it is evicted from memory). Default `600`. |
| `signingKeySizeBits` | RSA key size used to sign tokens. Default `2048`. |
| `signingKey` | Auto-generated and persisted on first run - do not set manually. |
