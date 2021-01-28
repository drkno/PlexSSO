# PlexSSO

![Docker Publish Status](https://github.com/drkno/PlexSSOv2/workflows/Publish%20Docker%20image/badge.svg)

An nginx `auth_request` Single Sign On service, using [Plex](https://plex.tv) as the upstream authorisation provider.

This is designed to sit in front of various services and replace their authentication with a single unified login. It is compatible with services such as:

- Bazarr
- Deluge
- Jackett
- Lidarr
- NzbHydra (v1 and v2)
- NzbGet
- Ombi
- Radarr
- Readarr
- Sabnzbd
- Sonarr
- Transcoderr
- Transmission

and more. Unlike other SSO providers such as [Organizr](https://github.com/causefx/Organizr) it is stand-alone so isn't tied to any usage pattern or front-end.

### Installation

1. Install `docker` and `nginx`. It is recommended that `nginx` is installed via a docker container.
2. Start this service in docker. This can be done with a command like `docker run -p 4200:4200/tcp --name plexsso -ti drkno/plexsso:latest -s 0123456789abcdef0123456789abcdef01234567`. See below for possible arguments and how to find their values.
3. Configure nginx to serve both `PlexSSO` and the upstream service(s). Every upstream service should have `auth_request` specified in it's configuration pointing to port `4200` of the SSO container. See the `/nginx` directory in this repository for examples.

### Configuration File

By default PlexSSO is configurable using a configuration stored in the `config.json` file. If a config is not found, a default one will be generated on startup. The location of this file can be overridden (see CLI Arguments).

`config.json` will look something like the following:  
```js
{
  "serverIdentifier": "0123456789abcdef0123456789abcdef01234567",
  "plexPreferencesFile": null,
  "cookieDomain": ".example.com",
  "defaultAccessDeniedMessage": "Access Denied",
  "accessControls": {
    "example-service": [
      {
        "path": "/",
        "blockMessage": "Access Denied.<br />Please use <a href='https://ombi.example.com'>Ombi</a> instead.",
        "minimumAccessTier": "NormalUser",
        "controlType": "Block",
        "exempt": [
          "some-exempt-user"
        ]
      }
    ]
  },
  "ombiPublicHostname": "https://ombi.example.com",
  "tautulliPublicHostname": "https://plexpy.example.com"
}
```

| Property              | Description |
|-----------------------|-------------|
| `serverIdentifier`    | Your plex server identifier. This can often be found somewhere in `/var/lib/plexmediaserver/Preferences.xml`. This argument is mandatory, as without it we do not know which server to authenticate against. |
| `plexPreferencesFile` | The path to your Plex `Preferences.xml` file, used to extract your Plex server identifier. This argument is relative to docker, so a volume must be configured in order to use this option. Additionally, it is mutually exclusive to `serverIdentifier` as it serves the same purpose. |
| `cookieDomain` | The domain to use for the authentication cookie. If all of your services are on subdomains `*.example.com` and your SSO is at `login.example.com` then this should be set to `.example.com`. See [MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies) for more information. |
| `accessControls` | A section for defining rules about which users are allowed to access which services. The default rule is that all users with access to your Plex server have access to all services. This section takes the form of a map/dictionary, with the service names being the key (as passed from `nginx`/other reverse proxy via the `X-PlexSSO-For` header) to list/array of rules. |
| `defaultAccessDeniedMessage` | The default message to show when an request is blocked but not by a rule. |

#### Access Control Service Rules
| Property              | Description |
|-----------------------|-------------|
| `path` | URL path within the affected service that this affects. Requires `X-PlexSSO-Original-URI` to be passed by `nginx`/your reverse proxy. |
| `minimumAccessTier` | Access tier that is required at minimum for this rule. If `controlType` is `Block`, then users with access levels less than this will be blocked, and >= will be allowed. If `controlType` is `Allow` the reverse applies. Possible values are `Owner`, `HomeUser`, `NormalUser` and `NoAccess`. |
| `controlType` | `Allow` or `Block`, changes the behaviour of `minimumAccessTier`. |
| `exempt` | Usernames of users which should have the decision made by `minimumAccessTier` reversed. |
| `blockMessage` | A custom message to deliver when access is denied due to this rule. Supports HTML. |
| `ombiPublicHostname` | The public facing hostname of Ombi (if present), must be reachable from PlexSSO. Will authenticate the user with Ombi using Ombi's native authentication allowing them to use their own account with the SSO. |
| `tautulliPublicHostname` | The public facing hostname of Tautulli/PlexPy (if present), must be reachable from PlexSSO. Will authenticate the user with Tautulli using Tautulli's native authentication allowing them to use their own account with the SSO. |

### CLI Arguments

_All CLI arguments have corresponding entries in the configuration file._

| Argument             | Description |
|----------------------|-------------|
| `--config`           | The directory to load the configuration from. Defaults to the current working directory or `/config/` if in Docker. |
| `-s`/`--server`      | See `serverIdentifier` in Configuration File section. |
| `-p`/`--preferences` | See `plexPreferencesFile` in Configuration File section. |
| `-c`/`--cookie-domain` | See `cookieDomain` in Configuration File section.  |
