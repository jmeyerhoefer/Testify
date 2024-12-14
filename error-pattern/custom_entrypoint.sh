#!/bin/sh
set -eu

cd /home/coder/Error-Pattern/
dotnet restore

git config --global --add safe.directory /home/coder/Error-Pattern

exec /usr/bin/entrypoint.sh "$@"