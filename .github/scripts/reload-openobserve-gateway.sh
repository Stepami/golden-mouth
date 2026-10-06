#!/usr/bin/env bash
set -euo pipefail

gateway_id="$(docker ps \
  --filter 'label=com.docker.compose.project=reki' \
  --filter 'label=com.docker.compose.service=gateway' \
  --format '{{.ID}}')"

if [[ -z "$gateway_id" || "$gateway_id" == *$'\n'* ]]; then
  echo 'Expected exactly one running reki gateway container.' >&2
  exit 1
fi

docker exec "$gateway_id" nginx -t
docker exec "$gateway_id" nginx -s reload
