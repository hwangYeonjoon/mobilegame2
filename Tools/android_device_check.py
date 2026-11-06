"""Collect device-only performance evidence without logs, account data or serial numbers."""
import argparse
import json
import subprocess
import time
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--adb', default='adb')
p.add_argument('--package', default='io.github.hwangyeonjoon.scrapguard')
p.add_argument('--output', type=Path, default=Path('TestResults/AndroidDevice'))
p.add_argument('--samples', type=int, default=1, help='For a 20 minute session use 21 samples at 60 seconds')
p.add_argument('--interval', type=int, default=60, help='Seconds between samples (1..60)')
a = p.parse_args()
if not 1 <= a.samples <= 61 or not 1 <= a.interval <= 60:
    p.error('samples must be 1..61 and interval 1..60')
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
emulator = run('shell', 'getprop', 'ro.kernel.qemu').strip() == '1'
start = time.monotonic()
samples = []
for index in range(a.samples):
    folder = a.output if a.samples == 1 else a.output / f'sample-{index:02d}'
    folder.mkdir(exist_ok=True)
    for name, command in {
        'frames.txt': ('shell', 'dumpsys', 'gfxinfo', a.package, 'framestats'),
        'memory.txt': ('shell', 'dumpsys', 'meminfo', a.package),
        'thermal.txt': ('shell', 'dumpsys', 'thermalservice'),
        'battery.txt': ('shell', 'dumpsys', 'battery'),
    }.items():
        (folder / name).write_text(run(*command))
    process = subprocess.run([a.adb, 'shell', 'pidof', a.package], capture_output=True, timeout=30)
    samples.append({'elapsed_seconds': round(time.monotonic()-start, 2), 'app_running': process.returncode == 0})
    print(f'Sample {index+1}/{a.samples} collected; emulator={emulator}', flush=True)
    if index+1 < a.samples:
        time.sleep(max(0, (index+1)*a.interval-(time.monotonic()-start)))
status = {'status': 'emulator_only' if emulator else 'evidence_collected', 'emulator': emulator,
          'model': run('shell', 'getprop', 'ro.product.model').strip(), 'samples': samples,
          'android_sdk': run('shell', 'getprop', 'ro.build.version.sdk').strip(),
          'limits': 'Manual review required. Unity GL frames may not be covered by gfxinfo; compare in-game frame samples. Touch, pause/resume, heat and sustained gameplay still need observation.'}
(a.output / 'status.json').write_text(json.dumps(status, indent=2) + '\n')
print('Evidence saved to', a.output)
