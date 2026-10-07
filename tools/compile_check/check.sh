#!/usr/bin/env bash
# Semantic compile check without Valheim's DLLs: Advanced.cs + CombatRuntime.cs + Skills.cs are
# compiled together (so calls into our own APIs are fully checked); Valheim/BepInEx types are
# stubbed (Stubs.cs), so errors about Valheim members are expected noise.
# The check compares error SIGNATURES (code + message, counts ignored) against a baseline commit
# known to compile on the user's PC; any NEW signature is a real bug.
# Usage: tools/compile_check/check.sh [baseline-commit]   (needs mono-mcs, curl, unzip)
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BASE="${1:-bada0dd}"
WORK="${TMPDIR:-/tmp}/ih_compile_check"
mkdir -p "$WORK/refs"
cd "$WORK/refs"
[ -d unity ] || { curl -sSL -o unity.nupkg https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg && unzip -qo unity.nupkg -d unity; }
[ -d harmony ] || { curl -sSL -o harmony.nupkg https://api.nuget.org/v3-flatcontainer/harmonyx/2.10.2/harmonyx.2.10.2.nupkg && unzip -qo harmony.nupkg -d harmony; }
REFS="$(ls "$WORK"/refs/unity/lib/net45/*.dll | sed 's/^/-r:/' | tr '\n' ' ') -r:$WORK/refs/harmony/lib/net45/0Harmony.dll"
FILES="AlbedosCustomClasses.Advanced.cs DragonsAltar.CombatRuntime.cs AlbedosCustomClasses.Skills.cs DragonsAltar.DevTools.cs AlbedosCustomClasses.GuardianAngel.cs DragonsAltar.Sorcerer.cs DragonsAltar.Ranger.cs"
rm -rf "$WORK/base" "$WORK/head"
mkdir -p "$WORK/base" "$WORK/head"
for F in $FILES; do
  git -C "$ROOT" show "$BASE:$F" > "$WORK/base/$F" 2>/dev/null || : > "$WORK/base/$F"
  cp "$ROOT/$F" "$WORK/head/$F"
done
for d in base head; do
  (cd "$WORK/$d" && mcs -langversion:5 -target:library $REFS -out:x.dll $FILES "$ROOT/tools/compile_check/Stubs.cs" > mcs.log 2>&1 || true)
done
# Core (Dragon's Altar) compiles separately: Stubs.cs fakes its Plugin class for the other modules.
grep -v "^namespace AlbedosCustomClasses {" "$ROOT/tools/compile_check/Stubs.cs" > "$WORK/StubsCore.cs"
git -C "$ROOT" show "$BASE:AlbedosCustomClasses.Core.cs" > "$WORK/base/AlbedosCustomClasses.Core.cs"
cp "$ROOT/AlbedosCustomClasses.Core.cs" "$WORK/head/AlbedosCustomClasses.Core.cs"
for d in base head; do
  (cd "$WORK/$d" && mcs -langversion:5 -target:library $REFS -out:core.dll AlbedosCustomClasses.Core.cs "$WORK/StubsCore.cs" "$ROOT/tools/compile_check/StubsCore.cs" > core.log 2>&1 || true)
done
python3 - "$WORK" "$ROOT" <<'PY'
import re, sys
w = sys.argv[1]
def load(p):
    out = {}
    for l in open(p):
        m = re.match(r'(.*?)\((\d+),\d+\): error (CS\d+): (.*)', l)
        if m: out.setdefault((m.group(3), m.group(4)), []).append(m.group(1).split('/')[-1] + ':' + m.group(2))
    return out
b, h = load(w + '/base/mcs.log'), load(w + '/head/mcs.log')
for k, v in load(w + '/base/core.log').items(): b.setdefault(k, []).extend(v)
for k, v in load(w + '/head/core.log').items(): h.setdefault(k, []).extend(v)
bad = [(k, v) for k, v in h.items() if k not in b]
# APIs whose modern overloads use System.ReadOnlySpan<T>: Windows PowerShell Add-Type cannot resolve them (v0.25.62).
import glob
import os
for f in sorted(glob.glob(os.path.join(sys.argv[2], '*.cs'))):
    for n, line in enumerate(open(f, encoding='utf-8', errors='ignore'), 1):
        code = line.split('//')[0]
        for api in ('.SetPositions(', '.LoadImage(', '.GetPositions('):
            if api in code: bad.append((('SPAN', 'Add-Type cannot resolve ' + api.strip('.(') + ' (Span overload)'), [os.path.basename(f) + ':' + str(n)]))
for k, v in bad: print('NEW ERROR', k[0], k[1], 'lines', v)
print('COMPILE CHECK', 'FAILED' if bad else 'PASSED', '(%d new signatures)' % len(bad))
sys.exit(1 if bad else 0)
PY
