#!/usr/bin/env python3
"""Edit locally recorded release-candidate gameplay; never uploads anything."""
import hashlib
import json
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / "TestResults/Promotion041"
OUT = ROOT / "Store/Promotions/robot-codex-041"
CUTS = [("assembly.mp4", 44, 3), ("navigation.mp4", 32, 5),
        ("battle.mp4", 146, 5), ("assembly.mp4", 0, 7)]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise SystemExit("ffmpeg is required")
    OUT.mkdir(parents=True, exist_ok=True)
    music = ROOT / "Assets/Resources/Audio/Licensed/Overdrive.ogg"
    args = [ffmpeg, "-hide_banner", "-loglevel", "error", "-y"]
    for name, start, duration in CUTS:
        args += ["-ss", str(start), "-t", str(duration), "-i", str(RAW / name)]
    args += ["-stream_loop", "-1", "-i", str(music)]
    filters = [f"[{i}:v:0]setpts=PTS-STARTPTS,fps=30,scale=540:960,setsar=1[v{i}]"
               for i in range(len(CUTS))]
    filters += ["[v0][v1][v2][v3]concat=n=4:v=1:a=0[v]",
                "[4:a]atrim=duration=20,asetpts=PTS-STARTPTS,volume=0.25,afade=t=out:st=18:d=2[a]"]
    portrait = OUT / "gameplay-portrait.mp4"
    codec = ["-c:v", "libx264", "-preset", "fast", "-crf", "20", "-threads", "2",
             "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", "-t", "20"]
    subprocess.run(args + ["-filter_complex", ";".join(filters), "-map", "[v]", "-map", "[a]"]
                   + codec + [str(portrait)], check=True)
    landscape = OUT / "gameplay-landscape.mp4"
    backdrop = ("[0:v]split[bg][fg];[bg]scale=1920:1080:force_original_aspect_ratio=increase,"
                "crop=1920:1080,gblur=sigma=35[blur];[fg]scale=-2:1080[front];"
                "[blur][front]overlay=(W-w)/2:(H-h)/2[v]")
    subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-i", str(portrait),
                    "-filter_complex", backdrop, "-map", "[v]", "-map", "0:a"]
                   + codec + [str(landscape)], check=True)
    manifest = {
        "package": "io.github.hwangyeonjoon.scrapguard", "version": "0.41.0", "versionCode": 45,
        "recorded_apk_sha256": "c7b143a8c7ad77159a555bd277bb13f1b984f40c4d93ef9702c5917976ffcd68",
        "environment": "Android API 35 ARM64 QA emulator, production package/server, actual input",
        "provenance": "Pre-final build45. Final changes only clarify carrier-only material sources and restore the completed-campaign home CTA; neither appears in this edit.",
        "editing": "20 seconds, cuts only, no speed change; 30fps container repeats source frames, not a performance claim. Landscape uses the same footage as a blurred backdrop. SRT captions are separate.",
        "audio": "Edited background music, not captured device audio: Overdrive by Alexander Ehlers / tricksntraps, CC0; see ArtSource/Audio/LICENSES.md.",
        "cuts": [{"file": name, "start_seconds": start, "duration_seconds": duration,
                  "source_sha256": digest(RAW / name)} for name, start, duration in CUTS],
        "outcome": "Real first-stage run: cannon4/4, specialized artillery crafted and ridden; voluntarily ended at1:43,443kills. Server accepted gold415/parts3. No victory fixture in video.",
        "outputs": [{"file": p.name, "bytes": p.stat().st_size, "sha256": digest(p)} for p in [portrait, landscape]],
    }
    (OUT / "edit-manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n")
    print(json.dumps(manifest["outputs"], indent=2))


if __name__ == "__main__":
    main()
