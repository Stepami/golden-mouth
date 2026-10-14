#!/usr/bin/env bash
set -euo pipefail
repo_root=$(cd "$(dirname "$0")/../.." && pwd)
fixture_dir=$(mktemp -d)
container_name="reki-static-check-$$"
# nginx workers must be able to traverse the read-only fixture mount.
chmod 755 "$fixture_dir"
cleanup() {
  docker rm -f "$container_name" >/dev/null 2>&1 || true
  rm -rf "$fixture_dir"
}
trap cleanup EXIT
mkdir -p "$fixture_dir/assets" "$fixture_dir/mock-backend/worlds" "$fixture_dir/runtime-config"
python3 - "$fixture_dir" <<'PY'
import json, pathlib, sys
root = pathlib.Path(sys.argv[1])
(root / 'assets/app.js').write_text('const welcome = "Реки: твоя история";\n' * 1000)
(root / 'assets/app.css').write_text('.card { color: white; background: #181820; }\n' * 1000)
(root / 'mock-backend/worlds/templates').write_text(json.dumps({'items': ['test'] * 4000}))
(root / 'runtime-config/config.js').write_text('window.__REKI_CONFIG__ = {};')
PY
cat > "$fixture_dir/mock.conf" <<'NGINX'
server {
  listen 8080;
  root /usr/share/nginx/html/mock-backend;
  default_type application/json;
}
NGINX
docker run --rm --network none --add-host backend:127.0.0.1 \
  -v "$repo_root/frontend/nginx.conf:/etc/nginx/conf.d/default.conf:ro" \
  nginx:1.29.1-alpine nginx -t
docker run -d --name "$container_name" --network none --add-host backend:127.0.0.1 \
  -v "$repo_root/frontend/nginx.conf:/etc/nginx/conf.d/default.conf:ro" \
  -v "$fixture_dir/mock.conf:/etc/nginx/conf.d/mock.conf:ro" \
  -v "$fixture_dir:/usr/share/nginx/html:ro" nginx:1.29.1-alpine >/dev/null
ready=false
for attempt in {1..20}; do
  if docker exec "$container_name" wget -q -O /dev/null http://127.0.0.1/runtime-config/config.js; then ready=true; break; fi
  sleep 0.2
done
if [[ "$ready" != true ]]; then
  docker logs "$container_name" >&2
  exit 1
fi
for asset in app.js app.css; do
  docker exec "$container_name" wget -q -S --header='Accept-Encoding: gzip' \
    -O "/tmp/$asset.gz" "http://127.0.0.1/assets/$asset" 2> "$fixture_dir/$asset.headers"
  docker cp "$container_name:/tmp/$asset.gz" "$fixture_dir/$asset.gz" >/dev/null
  docker exec "$container_name" wget -q -S --header='Accept-Encoding: identity' \
    -O "/tmp/$asset.raw" "http://127.0.0.1/assets/$asset" 2> "$fixture_dir/$asset.raw.headers"
  docker cp "$container_name:/tmp/$asset.raw" "$fixture_dir/$asset.raw" >/dev/null
done
docker exec "$container_name" wget -q -S --header='Accept-Encoding: gzip' \
  -O /tmp/api.json http://127.0.0.1/api/v1/worlds/templates 2> "$fixture_dir/api.headers"
docker exec "$container_name" wget -q -S \
  -O /dev/null http://127.0.0.1/runtime-config/config.js 2> "$fixture_dir/config.headers"
python3 - "$fixture_dir" <<'PY'
import gzip, pathlib, sys
root = pathlib.Path(sys.argv[1])
for name in ['app.js', 'app.css']:
    source = (root / 'assets' / name).read_bytes()
    compressed = (root / (name + '.gz')).read_bytes()
    headers = (root / (name + '.headers')).read_text().lower()
    raw_headers = (root / (name + '.raw.headers')).read_text().lower()
    assert 'content-encoding: gzip' in headers, headers
    assert 'vary: accept-encoding' in headers, headers
    assert 'immutable' in headers, headers
    assert 'content-encoding:' not in raw_headers, raw_headers
    assert gzip.decompress(compressed) == source
    assert (root / (name + '.raw')).read_bytes() == source
    assert len(compressed) < len(source), (name, len(source), len(compressed))
    print(f'PASS {name}: gzip {len(source)} -> {len(compressed)} bytes, identity unchanged')
api_headers = (root / 'api.headers').read_text().lower()
assert 'content-encoding:' not in api_headers, api_headers
assert 'no-store' in api_headers, api_headers
assert 'no-store' in (root / 'config.headers').read_text().lower()
print('PASS API compression disabled, API/config no-store retained')
PY
