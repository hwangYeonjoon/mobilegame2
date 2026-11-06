"""Render honest 5 fps simulation captures; no interpolated frames or fabricated combat."""
import argparse
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('folder', type=Path)
args = parser.parse_args()
evidence = sorted(args.folder.glob('run-*/evidence.json'))
if not evidence:
    parser.error('No natural combat captures found')
for path in evidence:
    data = json.loads(path.read_text())
    frames = sorted(path.parent.glob('frame-*.jpg'))
    if not frames:
        print(path.parent.name + ': no assembly clip (no assembly reached); no clip fabricated')
        continue
    output = args.folder / (path.parent.name + '-assembly.mp4')
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-framerate', '5',
                    '-i', str(path.parent / 'frame-%04d.jpg'), '-c:v', 'libx264',
                    '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(output)], check=True)
    print(f'{output}: {len(frames)/5:.1f}s, source=automated fresh-loadout combat, no audio, 5 fps')
