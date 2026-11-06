"""Compare signed AABs and bundletool download estimates for an explicit device profile."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--java', required=True)
p.add_argument('--bundletool', required=True, type=Path)
p.add_argument('--folder', type=Path, default=Path('TestResults/BuildSize'))
a = p.parse_args()
a.folder.mkdir(parents=True, exist_ok=True)
spec = a.folder / 'arm64-api35-ko-480dpi.json'
spec.write_text(json.dumps(dict(supportedAbis=['arm64-v8a'], supportedLocales=['ko', 'en'], screenDensity=480, sdkVersion=35)))
rows = []
for variant in ('baseline', 'optimized'):
    bundle = Path('Builds/Android/ScrapGuard-size-' + variant + '.aab')
    apks = a.folder / (variant + '.apks')
    run = subprocess.run([a.java, '-jar', str(a.bundletool), 'build-apks', '--bundle=' + str(bundle),
                          '--output=' + str(apks), '--device-spec=' + str(spec), '--overwrite'], capture_output=True, text=True)
    if run.returncode:
        raise SystemExit('bundletool build failed: ' + run.stderr[-2000:])
    size = subprocess.run([a.java, '-jar', str(a.bundletool), 'get-size', 'total', '--apks=' + str(apks),
                           '--device-spec=' + str(spec)], capture_output=True, text=True, check=True)
    numbers = [line for line in size.stdout.splitlines() if line and line[0].isdigit()]
    low, high = (int(x) for x in numbers[-1].split(','))
    report = json.loads((a.folder / ('ScrapGuard-internal-test-' + variant + '.json')).read_text())
    with zipfile.ZipFile(apks) as z:
        split_apk_bytes = sum(i.file_size for i in z.infolist() if i.filename.endswith('.apk'))
    rows.append(dict(variant=variant, aab_bytes=bundle.stat().st_size, sha256=hashlib.sha256(bundle.read_bytes()).hexdigest(),
                     music_packed_bytes=report['musicBytes'], split_apk_bytes=split_apk_bytes,
                     estimated_download_min_bytes=low, estimated_download_max_bytes=high))
result = dict(profile=json.loads(spec.read_text()), limits='bundletool estimate for this declared profile, not a Play Console measurement or physical installed footprint. Split APK total excludes extracted files and app data. APK sets are for analysis only; neither installed nor uploaded.', builds=rows)
(a.folder / 'comparison.json').write_text(json.dumps(result, indent=2) + '\n')
for r in rows:
    print(r['variant'], 'AAB MiB', round(r['aab_bytes']/2**20, 2), 'music MiB', round(r['music_packed_bytes']/2**20, 2),
          'download estimate MiB', round(r['estimated_download_max_bytes']/2**20, 2))
