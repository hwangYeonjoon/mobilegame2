"""Collect device-only performance evidence without logs, account data or serial numbers."""
import argparse
import json
import subprocess
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--adb', default='adb')
p.add_argument('--package', default='com.scrapguard.mobilegame2.prototype')
p.add_argument('--output', type=Path, default=Path('TestResults/AndroidDevice'))
a = p.parse_args()
def run(*args):
    r = subprocess.run([a.adb, *args], text=True, capture_output=True, timeout=30)
    if r.returncode:
        raise RuntimeError('adb command failed: ' + args[0])
    return r.stdout

a.output.mkdir(parents=True, exist_ok=True)
devices = [line.split() for line in run('devices').splitlines()[1:] if line.strip()]
ready = [line for line in devices if len(line) > 1 and line[1] == 'device']
if len(ready) != 1:
    (a.output / 'status.json').write_text(json.dumps({'status': 'not_tested', 'reason': 'Exactly one authorized Android device is required.'}, indent=2))
    raise SystemExit('No result: connect exactly one authorized Android device, run a battle, then retry.')
for name, command in {
    'frames.txt': ('shell', 'dumpsys', 'gfxinfo', a.package, 'framestats'),
    'memory.txt': ('shell', 'dumpsys', 'meminfo', a.package),
    'thermal.txt': ('shell', 'dumpsys', 'thermalservice'),
}.items():
    (a.output / name).write_text(run(*command))
status = {'status': 'evidence_collected', 'android_sdk': run('shell', 'getprop', 'ro.build.version.sdk').strip(),
          'limits': 'Manual review required. Unity GL frames may not be covered by gfxinfo; compare in-game frame samples. Touch, pause/resume, heat and sustained gameplay still need observation.'}
(a.output / 'status.json').write_text(json.dumps(status, indent=2) + '\n')
print('Evidence saved to', a.output)
