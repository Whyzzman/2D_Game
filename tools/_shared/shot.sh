# Спільне для tools/*/render.sh: браузер, сервер макетів і знімок сторінки в PNG.
# Підключати: source "$ROOT/tools/_shared/shot.sh"; потім serve <url> і shot <файл> <w> <h> <url> [бюджет мс].
# Інший Chromium: CHROME=<шлях>; для chrome-headless-shell (малює й при сплячому дисплеї) — HEADLESS=
if [[ -z "$CHROME" && ! -x "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" ]]; then
  # Без Chrome — headless-оболонка Playwright, якщо вона є (їй прапорець --headless не потрібен).
  CHROME="$(ls -d "$HOME"/Library/Caches/ms-playwright/chromium_headless_shell-*/chrome-headless-shell-mac-arm64/chrome-headless-shell 2>/dev/null | tail -1)"
  [[ -n "$CHROME" ]] && HEADLESS="${HEADLESS-}"
fi
CHROME="${CHROME:-/Applications/Google Chrome.app/Contents/MacOS/Google Chrome}"
HEADLESS="${HEADLESS---headless=new}"

serve() { # serve <url>: підняти сервер макетів, якщо він ще не відповідає
  curl -s -o /dev/null "$1" && return
  node "$ROOT/design/ui/_shared/serve.js" >/dev/null 2>&1 &
  SERVER=$!
  trap 'kill $SERVER 2>/dev/null' EXIT
  sleep 1
}

shot() { # shot <file> <w> <h> <url> [budget]
  # Chrome пише картинку й може сам не вийти: чекати на файл, потім зупинити його.
  local profile="$(mktemp -d)"
  rm -f "$1"
  "$CHROME" ${=HEADLESS} --disable-gpu --hide-scrollbars --default-background-color=00000000 \
    --user-data-dir="$profile" --no-first-run --no-default-browser-check \
    --window-size="$2,$3" --virtual-time-budget="${5:-6000}" --screenshot="$1" "$4" >/dev/null 2>&1 &
  local pid=$! waited=0
  while [[ ! -s "$1" && $waited -lt 600 ]]; do sleep 0.1; waited=$((waited + 1)); done
  sleep 0.3
  kill $pid 2>/dev/null; wait $pid 2>/dev/null
  rm -rf "$profile"
  [[ -s "$1" ]] || echo "failed: $1"
}
