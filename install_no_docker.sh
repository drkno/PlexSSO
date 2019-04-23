#!/bin/sh
# convenience install script for debian based OS
# requires apt, yarn, pip3, systemd

set -e

command -v apt >/dev/null 2>&1 || { echo >&2 "This script requires 'apt'."; exit 1; }
command -v yarn >/dev/null 2>&1 || { echo >&2 "This script requires 'yarn'."; exit 1; }
command -v pip3 >/dev/null 2>&1 || { echo >&2 "This script requires 'pip3'."; exit 1; }
command -v systemctl >/dev/null 2>&1 || { echo >&2 "This script requires 'systemctl'."; exit 1; }

[ "$EUID" -ne 0 ] && { echo >&2 "This script must run as root."; exit 1; }

# install nginx
apt install nginx -y
cp -r nginx/* /etc/nginx/conf.d/

# backend setup
mkdir -p /opt/sso
cp -r backend/* /opt/sso/
pip3 install -r /opt/sso/requirements.txt
rm /opt/sso/requirements.txt

# frontend setup
cd ui
yarn build
mv build /opt/sso/ui
cd ..

# systemd setup
cp sso.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable sso
systemctl start sso
systemctl reload nginx
