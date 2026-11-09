#!/bin/sh
# Dedicated headless QA AVD. Never wipes or modifies the logged-in Play Store AVD.
set -eu
scrap_sdk=${ANDROID_SDK_ROOT:-"$HOME/Unity/Editors/6000.3.7f1/6000.3.7f1/PlaybackEngines/AndroidPlayer/SDK"}
scrap_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
scrap_avd="$scrap_root/UserSettings/Android/ScrapGuard_QA_API35.avd"
export ANDROID_SDK_ROOT="$scrap_sdk"
export JAVA_HOME=${JAVA_HOME:-"$scrap_sdk/../OpenJDK"}
if [ ! -f "$scrap_avd/config.ini" ]; then
  "$scrap_sdk/cmdline-tools/16.0/bin/avdmanager" create avd --name ScrapGuard_QA_API35 \
    --package 'system-images;android-35;google_apis_playstore;arm64-v8a' --path "$scrap_avd" --device pixel_6
fi
python3 - "$scrap_avd/config.ini" <<'PY'
from pathlib import Path
import sys
p = Path(sys.argv[1])
settings = dict(line.split('=', 1) for line in p.read_text().splitlines() if '=' in line)
settings.update({'hw.lcd.width': '540', 'hw.lcd.height': '960', 'hw.lcd.density': '240', 'hw.keyboard': 'yes'})
p.write_text(''.join(k + '=' + v + '\n' for k, v in settings.items()))
PY
exec "$scrap_sdk/emulator/emulator" -avd ScrapGuard_QA_API35 -port 5556 \
  -no-window -no-snapshot -no-boot-anim -memory 2560 -cores 2 \
  -gpu "${SCRAP_QA_GPU:-swiftshader}" -feature -Vulkan -no-audio
