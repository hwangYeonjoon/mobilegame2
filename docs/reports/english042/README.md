# English 0.42.0 / 46 validation

- `verification.json`: test totals, Android environment and APK hash.
- `report.txt`: final Android QA, **0 errors**.
- `texts.txt`: rendered text from all **50** captured screens, including Korean switching and preserved user-authored names.
- Representative screenshots include title language selection, home, upgrades, outfits, Gem pack contents, growth supplies, Codex, assembler, battle and results.

Unity EditMode: **186 passed, 0 failed**. The local detailed XML is `TestResults/editmode-english-final.xml`.

Signed production AAB: `Builds/Android/ScrapGuard-0.42.0-46.aab` (also produced as `ScrapGuard-production.aab`). Bundletool validation and JAR signature verification passed. Manifest: production package, version 0.42.0 / 46, target SDK 36, non-debuggable. The self-signed upload certificate produces the usual Java trust-chain warning; the archive signature itself verifies. `launcher-names.txt` confirms English default and Korean localized names inside the actual AAB. Size and SHA-256 are recorded in `verification.json`.

Android: a fresh API 35 ARM64 QA emulator at 540×960. The existing QA device lacked install space; its QA files were backed up and a separate 8 GiB device was created. The logged-in user device and production app data were not used for these checks.

The UI runner uses a new local profile and no online purchases. It opens all ten region maps, both specialist/complete previews for representative robots, the five product descriptions and the core account/settings flows. The battle capture is an approximately 20-second runtime check followed by abandonment to exercise the result screen, not evidence of a completed campaign operation.

An earlier Android run found five overflow occurrences across four English phrases. Those phrases were shortened and all 50 screens were rerun. The checker covers untranslated Hangul (except intentional bilingual labels and protected names), text height and button hit targets. It does not establish that every possible server state or device size has been visually reviewed.

No real purchase, ad impression, overseas rollout or Play submission was performed. Emulator execution does not establish physical-device performance. Store listings and Google purchase-sheet translations remain drafts in `Store/en-US`.
