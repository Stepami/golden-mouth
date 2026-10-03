#!/bin/sh
# Скриншоты экранов фронта 390×844 для сравнения с docs/screens: scripts/screens.sh [коды...]
# Без аргументов снимает все экраны из галереи. Результат: frontend/shots/<код>.png (в git не идёт).
set -e
cd "$(dirname "$0")/.."
npm run build >/dev/null
PORT=4179; npx vite preview --port $PORT --strictPort >/dev/null 2>&1 & SRV=$!
trap 'kill $SRV 2>/dev/null' EXIT
sleep 2
CHROME="${CHROME:-/Applications/Google Chrome.app/Contents/MacOS/Google Chrome}"
CODES="$*"
[ -z "$CODES" ] && CODES=$(grep -ohE "'[0-9]{2}-[0-9a-z]+_[a-z0-9_]+'" src/screens/*.tsx src/screens/*/*.tsx 2>/dev/null | tr -d "'" | sort -u)
mkdir -p shots
for c in $CODES; do
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=3 --window-size=390,844 \
    --user-data-dir="$(mktemp -d)" --virtual-time-budget=4000 --screenshot="shots/$c.png" "http://localhost:$PORT/?screen=$c" >/dev/null 2>&1 &
  P=$!; i=0; while [ $i -lt 40 ] && [ ! -s "shots/$c.png" ]; do sleep .5; i=$((i+1)); done; sleep 1; kill $P 2>/dev/null || true
  echo "$c"
done
