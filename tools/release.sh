#!/bin/bash
# usage: tools/release.sh OLD NEW "Name" [nodefaults]   (bumps versions + banners, moves the old CHANGELOG)
set -e
OLD=$1; NEW=$2; NAME=$3
for f in *.cs; do sed -i "s/ModVersion = \"$OLD\"/ModVersion = \"$NEW\"/" "$f"; done
[ "$4" = "nodefaults" ] || sed -i "s/DefaultsPass = \"$OLD\"/DefaultsPass = \"$NEW\"/" DragonsAltar.DevTools.cs
UP=$(echo "$NAME" | tr a-z A-Z)
sed -i "2s/.*/title Immortal Heroes v$NEW - $NAME/" INSTALL.bat
sed -i "4s/.*/Write-Host \"IMMORTAL HEROES v$NEW - $UP\" -ForegroundColor Cyan/" INSTALL.ps1
[ -f CHANGELOG_v$OLD.txt ] && git mv CHANGELOG_v$OLD.txt docs/changelog/ || true
