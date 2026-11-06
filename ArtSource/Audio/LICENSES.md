# Audio sources — 0.22

Music by Alexander Ehlers (tricksntraps), **CC0 1.0**.
Source: https://opengameart.org/content/t-t-free-cyberpunk-pack-2
The creator page states CC0 and “Royalty Free Music by Alexander Ehlers”. Verified 2026-11-06.

- Factory.ogg: new_factory_129bpm_0.ogg
- Overdrive.ogg: ai_fight_120bpm.ogg
- Boss.ogg: electric_stream_180bpm.ogg

Downloaded directly from https://opengameart.org/sites/default/files/ with the above original filenames. Music source files unchanged; Unity imports/compresses and streams music. Regional base tracks, assembly and boss transitions crossfade in the player.

Sound effects by **Kenney**, **CC0 1.0**.
Creator license pages: https://kenney.nl/assets/impact-sounds and https://kenney.nl/assets/sci-fi-sounds and https://kenney.nl/assets/rpg-audio
Official creator ZIP distributions downloaded and verified on 2026-11-06. OriginalSfx files were replaced from these official archives; all 12 matched the previous copies byte for byte. Exact URLs, archive hashes and per-file comparisons are in official-provenance.json. Unmodified creator license files are in OfficialLicenses/. Runtime WAV files therefore need no sample replacement.

| File | Pack | Original |
| --- | --- | --- |
| Hit.wav | impact-sounds | impactMetal_light_000.ogg |
| Break.wav | impact-sounds | impactMetal_heavy_000.ogg |
| Pickup.wav | rpg-audio | handleCoins.ogg |
| Growth.wav | sci-fi-sounds | forceField_000.ogg |
| Shot.wav | sci-fi-sounds | laserSmall_000.ogg |
| Heavy.wav | sci-fi-sounds | laserLarge_000.ogg |
| Ultimate.wav | sci-fi-sounds | explosionCrunch_000.ogg |
| Assembly.wav | sci-fi-sounds | engineCircular_000.ogg |
| Shield.wav | sci-fi-sounds | forceField_001.ogg |
| Arc.wav | sci-fi-sounds | laserRetro_000.ogg |
| Frost.wav | sci-fi-sounds | laserSmall_002.ogg |
| Repair.wav | impact-sounds | impactMetal_medium_000.ogg |

License: https://creativecommons.org/publicdomain/zero/1.0/
Legal text: https://creativecommons.org/publicdomain/zero/1.0/legalcode
Attribution is included in the game audio options. Music files are renamed, not remixed. Effect originals are preserved in OriginalSfx/. Runtime effects are mono 44.1 kHz PCM WAV converted with FFmpeg and gain 0.6 to leave mixing headroom; no samples are synthesized. SHA-256 and byte sizes are in manifest.json. No payment or paid asset account was used. The previous generated SalvageDrive loop is retained as historical source and no longer selected for gameplay.

0.23 import update: music uses Vorbis quality 0.25 and 32,000 Hz, stereo streaming. Source OGG files and their hashes remain unchanged. The build-size audit reimports the previous quality 0.5 / preserved sample rate for a baseline signed AAB, then restores optimized import settings. Both archives use the existing internal-test signing path, not a relaxed production build guard; neither is uploaded automatically.
