"""Verify development APK packaging using Unity's Android SDK."""
from pathlib import Path
import hashlib
import json
import os
import re
import struct
import subprocess
import zipfile

root = Path(__file__).resolve().parents[1]
apk = root / 'Builds/Android/ScrapGuard-prototype.apk'
android = Path(os.environ.get('SCRAP_ANDROID_PLAYER', '/Users/kd-server/Unity/Editors/6000.3.7f1/6000.3.7f1/PlaybackEngines/AndroidPlayer'))
bin_dir = android / 'SDK/build-tools/36.0.0'
env = dict(os.environ, JAVA_HOME=str(android / 'OpenJDK'))
result_dir = root / 'TestResults'
result_dir.mkdir(exist_ok=True)

def run(args):
    result = subprocess.run([str(x) for x in args], env=env, text=True, capture_output=True, check=True)
    return result.stdout

badging = run([bin_dir / 'aapt', 'dump', 'badging', apk])
assert "versionName='0.16.0'" in badging and "versionCode='16'" in badging
assert "sdkVersion:'26'" in badging and "targetSdkVersion:'36'" in badging
assert "native-code: 'arm64-v8a'" in badging
manifest = run([bin_dir / 'aapt', 'dump', 'xmltree', apk, 'AndroidManifest.xml'])
assert 'androidx.core.content.FileProvider' in manifest
assert 'com.scrapguard.mobilegame2.prototype.showcase' in manifest
assert 'android.permission.INTERNET' in manifest
assert 'com.google.android.gms.ads.APPLICATION_ID' in manifest
assert 'ca-app-pub-3940256099942544~3347511713' in manifest
assert 'android.permission.WRITE_EXTERNAL_STORAGE' not in manifest
assert re.search(r'allowBackup[^\n]*0x0', manifest)
paths = run([bin_dir / 'aapt', 'dump', 'xmltree', apk, 'res/xml/showcase_paths.xml'])
assert paths.count('showcase/') >= 2
alignment = run([bin_dir / 'zipalign', '-c', '-P', '16', '-v', '4', apk])
assert 'Verification successful' in alignment
signature = run([bin_dir / 'apksigner', 'verify', '--verbose', apk])
assert 'Verified using v2 scheme (APK Signature Scheme v2): true' in signature
libraries = []
with zipfile.ZipFile(apk) as archive:
    for name in archive.namelist():
        if not name.startswith('lib/') or not name.endswith('.so'):
            continue
        data = archive.read(name)
        assert data[:6] == b'\x7fELF\x02\x01', name
        phoff, = struct.unpack_from('<Q', data, 32)
        size, count = struct.unpack_from('<HH', data, 54)
        load_alignments = []
        for index in range(count):
            offset = phoff + index * size
            p_type, = struct.unpack_from('<I', data, offset)
            if p_type == 1:
                align, = struct.unpack_from('<Q', data, offset + 48)
                assert align >= 16384, (name, align)
                load_alignments.append(align)
        assert load_alignments
        libraries.append({'name': name, 'load_alignments': load_alignments})
assert libraries
report = {'apk': str(apk), 'bytes': apk.stat().st_size, 'sha256': hashlib.sha256(apk.read_bytes()).hexdigest(),
          'version': '0.16.0', 'versionCode': 16, 'minSdk': 26, 'targetSdk': 36, 'abi': 'arm64-v8a',
          'v2_signature': True, 'zip_16kb': True, 'file_provider': True, 'backup_disabled': True,
          'libraries': libraries, 'device_execution': 'not tested'}
(result_dir / 'apk-016-verification.json').write_text(json.dumps(report, indent=2) + '\n')
(result_dir / 'apk-016-manifest.txt').write_text(manifest + '\n' + paths)
print(json.dumps({key:value for key,value in report.items() if key != 'libraries'}, indent=2))
print('ELF libraries checked:', len(libraries))
