#!/bin/sh
# Logic model only (no Unity / Valheim). Usage: tools/tests/nacc_handoff/run.sh
cd "$(dirname "$0")" && mcs -out:/tmp/nacc_model.exe Model.cs && mono /tmp/nacc_model.exe
