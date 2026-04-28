#!/bin/sh
set -eu

: "${RELAY_LISTEN_PORT:?RELAY_LISTEN_PORT is required}"
: "${RELAY_TARGET_HOST:?RELAY_TARGET_HOST is required}"
: "${RELAY_TARGET_PORT:?RELAY_TARGET_PORT is required}"

echo "Starting TCP relay ${RELAY_LISTEN_PORT} -> ${RELAY_TARGET_HOST}:${RELAY_TARGET_PORT}"

exec socat \
  TCP-LISTEN:${RELAY_LISTEN_PORT},fork,reuseaddr \
  TCP:${RELAY_TARGET_HOST}:${RELAY_TARGET_PORT}