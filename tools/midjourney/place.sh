#!/bin/zsh
# Puts Midjourney room backdrops in place of the painter's far layer (gamedesign/MIDJOURNEY.md).
# Usage: tools/midjourney/place.sh <zone>   e.g. tools/midjourney/place.sh tomb
# Reads design/levels/midjourney/<zone>/<room_id>.(png|jpg|jpeg|webp), crops it to the room's
# proportions from the centre and scales it to the size of Assets/Game/Art/Levels/<Zone>/Rooms/<room_id>_mid.png.
set -e
zmodload zsh/mathfunc
root="${0:A:h:h:h}"
zone=$1
[[ -z $zone ]] && { echo "usage: place.sh <zone>"; exit 1; }
Zone="${(C)zone}"
src="$root/design/levels/midjourney/$zone"
dst="$root/Assets/Game/Art/Levels/$Zone/Rooms"
placed=0
for file in "$src"/*.(png|jpg|jpeg|webp)(N); do
  room="${file:t:r}"
  target="$dst/${room}_mid.png"
  if [[ ! -f $target ]]; then
    echo "skip $room: no $target (wrong room id?)"
    continue
  fi
  tw=$(sips -g pixelWidth "$target" | awk '/pixelWidth/{print $2}')
  th=$(sips -g pixelHeight "$target" | awk '/pixelHeight/{print $2}')
  sw=$(sips -g pixelWidth "$file" | awk '/pixelWidth/{print $2}')
  sh=$(sips -g pixelHeight "$file" | awk '/pixelHeight/{print $2}')
  # Cover: scale so both sides reach the target, then cut the overflow from the centre.
  scale=$(( tw * 1.0 / sw > th * 1.0 / sh ? tw * 1.0 / sw : th * 1.0 / sh ))
  w=$(( int(ceil(sw * scale)) )); h=$(( int(ceil(sh * scale)) ))
  tmp=$(mktemp -t mj).png
  sips -s format png "$file" --out "$tmp" >/dev/null
  sips -z $h $w "$tmp" >/dev/null
  sips --cropToHeightWidth $th $tw "$tmp" --out "$target" >/dev/null
  rm -f "$tmp"
  echo "placed $room (${sw}x${sh} -> ${tw}x${th})"
  placed=$((placed + 1))
done
echo "$placed backdrop(s) placed in $dst"
