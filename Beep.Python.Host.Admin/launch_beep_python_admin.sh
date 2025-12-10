#!/usr/bin/env bash
set -euo pipefail

APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export BEEP_PYTHON_HOME="${BEEP_PYTHON_HOME:-${XDG_DATA_HOME:-$HOME/.local/share}/beep-python}"
mkdir -p "$BEEP_PYTHON_HOME"

export FLASK_ENV=production
export HOST="${HOST:-127.0.0.1}"
export PORT="${PORT:-5000}"

exec "$APP_DIR/BeepPythonHost" "$@"
