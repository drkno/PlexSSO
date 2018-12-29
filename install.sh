#!/bin/sh
# convenience install script for debian based OS
# requires apt, yarn, pip3, systemd
# expected to be run during a docker build

set -e

apt install nginx -y
cp nginx.conf /etc/nginx/conf.d/
cd ui
yarn build
mkdir -p /opt/sso
mv build /opt/sso/ui
chmod -R o+x /opt/sso/ui
mv cp -r backend /opt/sso/backend
pip3 install cachetools
cp sso.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable sso
systemctl start sso
systemctl reload nginx
