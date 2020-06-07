# PlexSSO

![Docker Cloud Build Status](https://img.shields.io/docker/cloud/build/drkno/plexsso?style=flat-square)

An nginx `auth_request` Single Sign On service, using [Plex](https://plex.tv) as the upstream authorisation provider.

This is designed to sit in front of various services and replace their authentication with a single unified login. It is compatible with services such as:

- Sonarr
- Radarr
- Deluge
- Sabnzbd
- NzbHydra (v1 and v2)
- Jackett

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
  "accessControls": {
    "example-service": [
      {
        "path": "/",
        "minimumAccessTier": "NormalUser",
        "controlType": "Block",
        "exempt": [
          "some-exempt-user"
        ]
      }
    ]
  }
}
```

| Property              | Description |
|-----------------------|-------------|
| `serverIdentifier`    | Your plex server identifier. This can often be found somewhere in `/var/lib/plexmediaserver/Preferences.xml`. This argument is mandatory, as without it we do not know which server to authenticate against. |
| `plexPreferencesFile` | The path to your Plex `Preferences.xml` file, used to extract your Plex server identifier. This argument is relative to docker, so a volume must be configured in order to use this option. Additionally, it is mutually exclusive to `serverIdentifier` as it serves the same purpose. |
| `cookieDomain` | The domain to use for the authentication cookie. If all of your services are on subdomains `*.example.com` and your SSO is at `login.example.com` then this should be set to `.example.com`. See [MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies) for more information. |
| `accessControls` | A section for defining rules about which users are allowed to access which services. The default rule is that all users with access to your Plex server have access to all services. This section takes the form of a map/dictionary, with the service names being the key (as passed from `nginx`/other reverse proxy via the `X-PlexSSO-For` header) to list/array of rules. |

#### Access Control Service Rules
| Property              | Description |
|-----------------------|-------------|
| `path` | URL path within the affected service that this affects. Requires `X-PlexSSO-Original-URI` to be passed by `nginx`/your reverse proxy. |
| `minimumAccessTier` | Access tier that is required at minimum for this rule. If `controlType` is `Block`, then users with access levels less than this will be blocked, and >= will be allowed. If `controlType` is `Allow` the reverse applies. Possible values are `Owner`, `HomeUser`, `NormalUser` and `NoAccess`. |
| `controlType` | `Allow` or `Block`, changes the behaviour of `minimumAccessTier`. |
| `exempt` | Usernames of users which should have the decision made by `minimumAccessTier` reversed. |

### CLI Arguments

_All CLI arguments have corresponding entries in the configuration file._

| Argument             | Description |
|----------------------|-------------|
| `--config`           | The location to load the configuration file from. Defaults to `config.json` in the current working directory or `/config/config.json` if in Docker. |
| `-s`/`--server`      | See `serverIdentifier` in Configuration File section. |
| `-p`/`--preferences` | See `plexPreferencesFile` in Configuration File section. |
| `-c`/`--cookie-domain` | See `cookieDomain` in Configuration File section.  |
