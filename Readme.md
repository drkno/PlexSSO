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

### CLI Arguments

| Argument             | Description |
|----------------------|-------------|
| `-s`/`--server`      | Your plex server identifier. This can often be found somewhere in `/var/lib/plexmediaserver/Preferences.xml`. This argument is mandatory, as without it we do not know which server to authenticate against. |
| `-p`/`--preferences` | The path to your Plex `Preferences.xml` file, used to extract your Plex server identifier. This argument is relative to docker, so a volume must be configured in order to use this option. Additionally, it is mutually exclusive to `--server` as it serves the same purpose. |
| `-c`/`--cookie-domain` | The domain to use for the authentication cookie. If all of your services are on subdomains `*.example.com` and your SSO is at `login.example.com` then this should be set to `.example.com`. See [MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies) for more information.  |