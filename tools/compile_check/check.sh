#!/usr/bin/env bash
# Semantic compile check for AlbedosCustomClasses.Advanced.cs without Valheim's DLLs.
# Valheim/BepInEx types are stubbed (Stubs.cs), so stub-related errors are expected noise.
# The check compares error signatures against a baseline commit that is known to compile
# on the user's PC; any NEW error signature is a real bug.
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
F=AlbedosCustomClasses.Advanced.cs
mkdir -p "$WORK/base" "$WORK/head"
git -C "$ROOT" show "$BASE:$F" > "$WORK/base/$F"
cp "$ROOT/$F" "$WORK/head/$F"
for d in base head; do
  (cd "$WORK/$d" && mcs -langversion:5 -target:library $REFS -out:x.dll "$F" "$ROOT/tools/compile_check/Stubs.cs" > mcs.log 2>&1 || true)
done
python3 - "$WORK" <<'PY'
import re, sys
w = sys.argv[1]
def load(p):
    out = {}
    for l in open(p):
        m = re.match(r'.*\((\d+),\d+\): error (CS\d+): (.*)', l)
        if m: out.setdefault((m.group(2), m.group(3)), []).append(int(m.group(1)))
    return out
b, h = load(w + '/base/mcs.log'), load(w + '/head/mcs.log')
bad = [(k, v) for k, v in h.items() if k not in b or len(v) > len(b[k])]
for k, v in bad: print('NEW ERROR', k[0], k[1], 'lines', v)
print('COMPILE CHECK', 'FAILED' if bad else 'PASSED', '(%d new signatures)' % len(bad))
sys.exit(1 if bad else 0)
PY
