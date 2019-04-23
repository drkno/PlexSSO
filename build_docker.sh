set -e

docker build --no-cache -t drkno/plexsso .
docker push drkno/plexsso
