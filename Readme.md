# PlexSSO (v2)
Version 2 of my SSO for Plex, designed to handoff all authentication matters to [plex.tv](https://plex.tv), like PlexPy/Tautulli does.

This version does not pass authentication headers to the various applications, however is much easier to dockerise.

### Usage
1. Install docker, nginx
2. Start the docker container. If using docker, the `-s <plexserveridentifierhere>` argument must be passed. Your plex server identifier can be located in the Plex `Preferences.xml` file (often somewhere in `/var/lib/plexmediaserver`). E.g. run the command `docker run -p 4200:4200/tcp -ti mrkno/plexsso -s 0123456789abcdef0123456789abcdef01234567`.
3. Look in the nginx examples in this repository for configuration examples
