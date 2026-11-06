"""Verify Unity-produced replay fixtures with the authoritative .NET simulation."""
import argparse
import json
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folder', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    dll = root / 'Tools/CoreChecks/bin/Debug/net10.0/CoreChecks.dll'
    files = sorted(set(args.folder.glob('probe-*.json')) | set(args.folder.glob('runtime-replay-*.json')))
    if not files or not dll.is_file():
        parser.error('Build Tools/CoreChecks and supply a folder containing Unity replay fixtures.')
    results = []
    for path in files:
        run = subprocess.run(['dotnet', str(dll), '--verify-replay', str(path.resolve())],
                             capture_output=True, text=True, timeout=90)
        detail = (run.stdout + run.stderr).strip()
        results.append(dict(file=path.name, passed=run.returncode == 0, detail=detail))
        print(path.name + ': ' + detail)
    report = dict(total=len(results), passed=sum(r['passed'] for r in results), cases=results)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    return 0 if report['passed'] == report['total'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
