#!/bin/sh
# Double-click in Finder to start the existing Play Store virtual device.
set -eu
cd "$(dirname "$0")/.."
exec sh Tools/start_android_emulator.sh
