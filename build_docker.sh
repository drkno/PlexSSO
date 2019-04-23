set -e

docker build --no-cache -t mrkno/plexsso .
docker push mrkno/plexsso
