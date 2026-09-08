#!/usr/bin/env bash
set -euo pipefail
DEST="$(cd "$(dirname "$0")/.." && pwd)/TongaKids/Resources/Fonts"
mkdir -p "$DEST"
UA="Mozilla/5.0"
CURL=(curl -sSfL --http1.1 --retry 3 --retry-delay 1 --retry-all-errors -A "$UA")

fetch_weight() {
  local weight="$1" out="$2"
  local css url
  css=$("${CURL[@]}" "https://fonts.googleapis.com/css2?family=Nunito+Sans:wght@${weight}")
  url=$(echo "$css" | grep -o 'https://fonts.gstatic.com/[^)]*' | head -1)
  [ -n "$url" ] || { echo "no URL for weight $weight" >&2; exit 1; }
  "${CURL[@]}" -o "$DEST/$out" "$url"
  echo "  $out"
}

echo "Nunito Sans:"
fetch_weight 400 NunitoSans-Regular.ttf
fetch_weight 500 NunitoSans-Medium.ttf
fetch_weight 700 NunitoSans-Bold.ttf
fetch_weight 800 NunitoSans-ExtraBold.ttf

echo "Material Symbols:"
css=$("${CURL[@]}" "https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined")
url=$(echo "$css" | grep -o 'https://fonts.gstatic.com/[^)]*' | head -1)
"${CURL[@]}" -o "$DEST/MaterialSymbolsOutlined.ttf" "$url"
echo "  MaterialSymbolsOutlined.ttf"
