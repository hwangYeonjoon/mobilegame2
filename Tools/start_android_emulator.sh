#!/bin/sh
# Reopen the installed Play Store AVD; never wipes its Google account or app data.
set -eu
scrap_sdk=${ANDROID_SDK_ROOT:-"$HOME/Unity/Editors/6000.3.7f1/6000.3.7f1/PlaybackEngines/AndroidPlayer/SDK"}
if [ ! -x "$scrap_sdk/emulator/emulator" ]; then
  echo 'Set ANDROID_SDK_ROOT to the SDK containing the installed Android emulator.' >&2
  exit 1
fi
export ANDROID_SDK_ROOT="$scrap_sdk"
exec "$scrap_sdk/emulator/emulator" -avd ScrapGuard_Play_API35 \
  -no-snapshot -no-boot-anim -memory 2560 -cores 2 -gpu swiftshader -feature -Vulkan -no-audio
