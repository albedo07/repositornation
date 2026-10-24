#!/bin/bash
# Real-reference compile with the SAME reference set as INSTALL.ps1 (BepInEx, assembly_valheim, netstandard,
# SoftReferenceableAssets, UnityEngine*; NOT assembly_utils) and its auto-add rule: a dependency is added only when the
# compiler error names "assembly 'X'" (v0.25.265: an extension method from assembly_utils passed here but failed in the
# installer). Jotunn stubbed for Core only. Usage: realbuild.sh SRC_DIR
S=/tmp/claude-0/-home-user-repositornation/7735db43-7259-5352-84c4-42e32412eaf9/scratchpad; R=$S/realrefs; B=$S/cc/refs/bepz/BepInEx/core; O=$S/rb; SRC=$1
COMMON="-r:$B/BepInEx.dll -r:$R/assembly_valheim.dll -r:$R/SoftReferenceableAssets.dll $(ls $R/UnityEngine*.dll | sed 's/^/-r:/' | tr '\n' ' ') -r:/usr/lib/mono/4.5/Facades/netstandard.dll"
H="-r:$B/0Harmony.dll"
c() {
  out=$1; shift; extra=""
  for attempt in 1 2 3 4 5 6; do
    log=$(mcs -langversion:5 -nowarn:618,612,414,169,649,219,168,162,1522 -target:library -out:$O/$out "$@" $extra 2>&1)
    # the installer's csc can report a non-CS0012 error alone (no assembly named) -> it fails; so do we
    if echo "$log" | grep "error CS" | grep -qv "error CS0012"; then break; fi
    names=$(echo "$log" | grep "error" | grep -o "assembly \`[^,\`']*" | sed "s/assembly \`//" | sort -u)
    [ -z "$names" ] && break
    added=0
    for n in $names; do [ -f "$R/$n.dll" ] && [[ "$extra" != *"$R/$n.dll"* ]] && { extra="$extra -r:$R/$n.dll"; echo "  auto-added $n.dll"; added=1; }; done
    [ $added = 0 ] && break
  done
  echo "$log" | grep -E "error|Compilation"
}
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
