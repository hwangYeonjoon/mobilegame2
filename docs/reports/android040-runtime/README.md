# 0.40 final Android QA evidence

2026-11-11 / rules 30 / version 0.40.0 (43). Dedicated emulator-5556, arm64 IL2CPP. No physical-device, human-play, or real-billing claim.

The app completed and exited before the final files were pulled. UI errors: 0. Four final runtime recordings replayed on .NET: 4/4 exact matches. 31 screenshots were produced; nine selected images are preserved here. Source records are gzip-compressed; decompress to an ignored TestResults directory and run `python3 Tools/verify_runtime_replays.py <directory> --output <report.json>`.

The isolated QA fixture contains 600 test gems, earned-mastery fixture data, and offline commands. This checks purchase confirmation/debit/equip and preview progress isolation, not actual payment conversion. APK static manifests say device execution not tested because that tool inspects binaries only; the separate runtime evidence above covers emulator execution.

QA and manual playtest APKs share the isolated `.qa` package. They do not replace the Play test app. The manual APK has no automatic smoke routine. APK hashes and signature/16 KB alignment checks are preserved in the adjacent JSON files.
