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
    args = parser.parse_args()
    env = os.environ.copy()
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
                            'ScrapGuard.Editor.ProjectBuilder.BuildAndroidBundle'],
                           cwd=ROOT, env=env)
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
    print('Internal-test AAB created:', bundle)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
