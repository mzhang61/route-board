#!/usr/bin/env bash
set -euo pipefail
umask 077

if [[ $# -ne 2 || ! "$1" =~ ^(staging|production)$ ]]; then
  echo "Usage: deploy/release.sh staging|production /absolute/path/release.env" >&2
  exit 2
fi

environment="$1"
release_file="$2"
script_directory="$(cd "$(dirname "$0")" && pwd)"
if [[ "$release_file" != /* || ! -f "$release_file" ]]; then
  echo "The release environment file must exist at an absolute path." >&2
  exit 2
fi

compose=(docker compose --project-name "routeboard-$environment"
  --env-file "$release_file" -f "$script_directory/compose.yaml")

# Validate required settings without printing expanded secrets or configuration.
"${compose[@]}" config --quiet
"${compose[@]}" up -d --wait --wait-timeout 120 db
"${compose[@]}" --profile release run --rm migrate
"${compose[@]}" exec -T db psql -U routeboard_migrator -d routeboard \
  --set ON_ERROR_STOP=1 < "$script_directory/runtime-grants.sql"
"${compose[@]}" up -d api web

# Check through the same ingress as users, including public certificate validation.
# A failed health check returns nonzero so the pipeline must stop promotion.
"${compose[@]}" exec -T web sh -c '
  for attempt in 1 2 3 4 5 6 7 8 9 10; do
    if wget -q -O /dev/null "https://$SITE_ADDRESS/healthz"; then
      exit 0
    fi
    sleep 3
  done
  exit 1
'
echo "Release health check passed."
