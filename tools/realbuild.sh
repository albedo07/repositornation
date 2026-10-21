#!/bin/bash
# Real-reference compile, same order/refs as INSTALL.ps1 (Jotunn stubbed for Core only). Usage: realbuild.sh SRC_DIR
S=/tmp/claude-0/-home-user-repositornation/7735db43-7259-5352-84c4-42e32412eaf9/scratchpad; R=$S/realrefs; B=$S/cc/refs/bepz/BepInEx/core; O=$S/rb; SRC=$1
COMMON="$(ls $R/*.dll | sed 's/^/-r:/' | tr '\n' ' ') -r:$B/BepInEx.dll -r:/usr/lib/mono/4.5/Facades/netstandard.dll"
H="-r:$B/0Harmony.dll"
c() { out=$1; shift; mcs -langversion:5 -nowarn:618,612,414,169,649,219,168,162,1522 -target:library -out:$O/$out "$@" 2>&1 | grep -E "error|Compilation" ; }
echo "== Runtime"; c AlbedosCustomClasses.CombatRuntime.dll $COMMON $H $SRC/DragonsAltar.CombatRuntime.cs
echo "== Skills"; c AlbedosCustomClasses.Skills.dll $COMMON $H -r:$O/AlbedosCustomClasses.CombatRuntime.dll $SRC/AlbedosCustomClasses.Skills.cs
echo "== Passives"; c AlbedosCustomClasses.Passives.dll $COMMON -r:$O/AlbedosCustomClasses.CombatRuntime.dll $SRC/AlbedosCustomClasses.Passives.cs
echo "== Guardian"; c AlbedosCustomClasses.GuardianAngel.dll $COMMON $H -r:$O/AlbedosCustomClasses.CombatRuntime.dll $SRC/AlbedosCustomClasses.GuardianAngel.cs
J=$S/jot/ValheimModding-Jotunn/Jotunn.dll
echo "== Core"; c AlbedosCustomClasses.Core.dll $COMMON -r:$J $SRC/AlbedosCustomClasses.Core.cs
for m in Advanced Sorcerer Ranger; do f=$SRC/AlbedosCustomClasses.$m.cs; [ -f $f ] || f=$SRC/DragonsAltar.$m.cs
echo "== $m"; c X.$m.dll $COMMON $H -r:$O/AlbedosCustomClasses.Core.dll -r:$O/AlbedosCustomClasses.CombatRuntime.dll -r:$O/AlbedosCustomClasses.Skills.dll $f; done
echo "== DevTools"; c X.DevTools.dll $COMMON $SRC/DragonsAltar.DevTools.cs
echo "== Experience"; c X.Exp.dll $COMMON $H $SRC/Aethelborn.Experience.cs
