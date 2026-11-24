#!/bin/zsh
set -eu
cd "$(dirname "$0")/.."
python3 -m Tools.launch_dashboard "$@"
/usr/bin/open UserSettings/Operations/index.html
