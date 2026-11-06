#!/usr/bin/env python3
"""Build the internal-test bundle using local signing secrets, never command arguments."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import time
import zipfile

ROOT = Path(__file__).resolve().parents[1]
KEYS = ('SCRAP_KEYSTORE_PATH', 'SCRAP_KEY_ALIAS',
        'SCRAP_KEYSTORE_PASSWORD', 'SCRAP_KEY_PASSWORD')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Check local inputs without building')
    audio = parser.add_mutually_exclusive_group()
    audio.add_argument('--audio-baseline', action='store_true', help='Measure the previous music import settings using the same signed build')
    audio.add_argument('--audio-optimized', action='store_true', help='Reimport optimized music and measure the same signed build')
    args = parser.parse_args()
    env = os.environ.copy()
    if args.audio_baseline or args.audio_optimized:
        env.pop("SCRAP_AUDIO_BASELINE", None)
    credentials = ROOT / 'UserSettings/Signing/credentials.json'
    if not all(env.get(key) for key in KEYS) and credentials.exists():
        if os.name == 'posix' and credentials.stat().st_mode & 0o077:
            raise SystemExit('Signing credentials must be private (chmod 600).')
        try:
            local = json.loads(credentials.read_text())
        except (ValueError, OSError):
            raise SystemExit('Could not read the local signing configuration.') from None
        # Use one complete source so aliases/passwords from different keys are not mixed.
        for key in KEYS:
            env[key] = local.get(key, '')
    if not all(isinstance(env.get(key), str) and env[key] for key in KEYS):
        raise SystemExit('Provide all four SCRAP signing variables or local credentials.json.')
    if not Path(env['SCRAP_KEYSTORE_PATH']).is_file():
        raise SystemExit('The configured upload keystore does not exist.')
    if args.check:
        print('Local signing inputs present. No secret values printed; no build or upload performed.')
        return 0
    unity = shutil.which('unity') or str(Path.home() / '.unity/bin/unity')
    if not Path(unity).is_file():
        raise SystemExit('Unity CLI not found.')
    log = ROOT / 'TestResults/android-internal.log'
    log.parent.mkdir(parents=True, exist_ok=True)
    started = time.time()
    result = subprocess.call([unity, 'run', str(ROOT), '--no-tail', '--timeout', '1800',
                            '--log-file', str(log), '--', '-nographics', '-buildTarget',
                            'Android', '-executeMethod',
                            ('ScrapGuard.Editor.BuildFootprint.BuildBaseline' if args.audio_baseline else 'ScrapGuard.Editor.BuildFootprint.BuildOptimized' if args.audio_optimized else 'ScrapGuard.Editor.ProjectBuilder.BuildAndroidBundle')],
                           cwd=ROOT, env=env)
    if args.audio_baseline or args.audio_optimized:
        if log.exists():
            shutil.copy2(log, log.with_name("audio-" + ("baseline" if args.audio_baseline else "optimized") + "-build.log"))
    if result:
        return result
    bundle = ROOT / 'Builds/Android/ScrapGuard-internal-test.aab'
    if not bundle.is_file() or bundle.stat().st_mtime < started:
        raise SystemExit('No fresh AAB produced. Inspect TestResults/android-internal.log.')
    try:
        with zipfile.ZipFile(bundle) as archive:
            names = set(archive.namelist())
            if not {'BundleConfig.pb', 'base/manifest/AndroidManifest.xml'} <= names:
                raise SystemExit('Output is missing the Android App Bundle structure.')
            if archive.testzip() is not None:
                raise SystemExit('Output contains a corrupt ZIP entry.')
    except zipfile.BadZipFile:
        raise SystemExit('Output is not a valid Android App Bundle ZIP.') from None
    if args.audio_baseline or args.audio_optimized:
        snapshot = bundle.with_name('ScrapGuard-size-' + ('baseline' if args.audio_baseline else 'optimized') + '.aab')
        shutil.copy2(bundle, snapshot)
        print('Size comparison AAB saved:', snapshot)
    print('Internal-test AAB created:', bundle)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
