#!/bin/bash

set -e

echo '////////////////////////////////////////////////////////////////////////////////'
echo '// Removing systemd service unit'
echo '////////////////////////////////////////////////////////////////////////////////'
sudo rm '/lib/systemd/system/own-blockchain-public-node@.service'
sudo systemctl daemon-reload

echo '////////////////////////////////////////////////////////////////////////////////'
echo '// Removing node binaries'
echo '////////////////////////////////////////////////////////////////////////////////'
sudo rm -rf /opt/own/blockchain/public/node

echo '////////////////////////////////////////////////////////////////////////////////'
echo '// Removing database'
echo '////////////////////////////////////////////////////////////////////////////////'
INSTANCE_NAME=ins1
INSTANCE_DB=own_public_blockchain_$INSTANCE_NAME
INSTANCE_USER=${INSTANCE_DB}_user
sudo -u postgres psql << EOF
\set ON_ERROR_STOP on

\c postgres

DROP DATABASE $INSTANCE_DB;
DROP USER $INSTANCE_USER;
EOF

echo '////////////////////////////////////////////////////////////////////////////////'
echo '// Removing data and configuration'
echo '////////////////////////////////////////////////////////////////////////////////'
sudo rm -rf /var/lib/own/blockchain/public/node