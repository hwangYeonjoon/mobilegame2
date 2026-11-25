#!/usr/bin/env python3
"""Fail when a shipped Korean literal has no English catalog entry.

Presentation keys remain Korean; gameplay IDs and saved data never change.
Run from any directory. QA diagnostics, regexes and the Korean language's
native name are not translations. Runtime smoke also checks composed text.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HANGUL = re.compile(r"[가-힣ㄱ-ㆎ]")
LITERAL = re.compile(r'"(?:[^"\\]|\\.)*"')
EXEMPT = {"한국어", "^[가-힣a-zA-Z0-9 _-]{2,12}$"}


def main():
    entries = json.loads((ROOT / "Assets/Resources/Localization/en.json").read_text())["entries"]
    keys, errors = set(), []
    for entry in entries:
        ko, en = entry["ko"], entry["en"]
        if ko in keys or not ko or (not en and ko != "개"):
            errors.append(f"Invalid/duplicate catalog entry: {ko!r}")
        keys.add(ko)
        if HANGUL.search(en) and ko != "Language / 언어":
            errors.append(f"Korean remains in English: {ko!r}")
    for folder in ("Assets/Game/Runtime", "Assets/Game/Core", "Server/ScrapGuard.Server"):
        for path in (ROOT / folder).rglob("*.cs"):
            if any(s in path.name for s in ("Smoke", "NaturalCombat", "Localization")) or "obj" in path.parts:
                continue
            for match in LITERAL.finditer(path.read_text()):
                try:
                    source = json.loads(match.group())
                except ValueError:
                    continue
                if HANGUL.search(source) and source not in keys | EXEMPT:
                    line = path.read_text()[:match.start()].count("\n") + 1
                    errors.append(f"{path.relative_to(ROOT)}:{line}: missing {source!r}")
    print(f"English catalog: {len(entries)} entries; {len(errors)} errors")
    print("\n".join(errors))
    return bool(errors)


if __name__ == "__main__":
    sys.exit(main())
