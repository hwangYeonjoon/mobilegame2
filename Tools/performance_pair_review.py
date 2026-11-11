"""Summarize the matched 0.37/0.38 Android fixed-work experiment."""
import argparse
import json
from pathlib import Path
from statistics import median


def read(folder):
    rows = [json.loads(p.read_text()) for p in folder.glob('paired-*.json')]
    expected = {(r, p) for r in range(3) for p in ('opening', 'crowded')}
    indexed = {(r['repeat'], r['phase']): r for r in rows}
    if len(rows) != 6 or set(indexed) != expected:
        raise ValueError('Expected exactly three complete opening/crowded pairs')
    if (folder / 'report.txt').read_text().strip() != 'Performance errors: 0':
        raise ValueError('Runtime did not finish successfully')
    return indexed


def report(before, after):
    a, b = read(before), read(after)
    for key in a:
        if (a[key]['build'], a[key]['rules'], b[key]['build'], b[key]['rules']) != (40, 27, 41, 28):
            raise ValueError('Unexpected baseline versions')
        for field in ('frames', 'hash', 'kills', 'peakEnemies', 'simulationSeconds', 'gpu', 'kind'):
            if a[key][field] != b[key][field]:
                raise ValueError(f'Workload mismatch: {key} {field}')
        if a[key]['frames'] != 1800:
            raise ValueError('Incomplete window')
    lines = ['# 0.37 / 0.38 동일 부하 Android 비교', '',
        '동일 M1 host 에뮬레이터, 고정 틱 1회/렌더 프레임, 간결 효과. 각 버전 일반·밀집 3회씩. 실제 게임 FPS 또는 실물 폰 성능의 합격 판정이 아니다.', '',
        '모든 대응 표본의 틱 수·처치·최대 적·진행 시간·진단 해시가 일치했다. VFX 위치와 OS/GPU 스케줄링은 고정하지 않았다.', '',
        '| 구간 | 반복 | 처리 프레임/초 전→후 | 프레임 p99 ms 전→후 | 코어 CPU p99 ms 전→후 | 장면 갱신 CPU p99 ms 전→후 |',
        '|---|---:|---:|---:|---:|---:|']
    for key in sorted(a):
        old, new = a[key], b[key]
        fps = lambda r: r['frames'] / r['wallSeconds']
        pair = lambda field: f"{old[field]:.2f}→{new[field]:.2f}"
        lines.append(f"| {key[1]} | {key[0]+1} | {fps(old):.1f}→{fps(new):.1f} | {pair('frameP99')} | {pair('coreP99')} | {pair('sceneP99')} |")
    lines += ['', '| 구간 | 처리량 중앙값 전→후 | 프레임 p99 중앙값 전→후 |', '|---|---:|---:|']
    for phase in ('opening', 'crowded'):
        rows = [[r for r in collection.values() if r['phase'] == phase] for collection in (a, b)]
        f = [median(r['frames']/r['wallSeconds'] for r in rs) for rs in rows]
        p = [median(r['frameP99'] for r in rs) for rs in rows]
        lines.append(f'| {phase} | {f[0]:.1f}→{f[1]:.1f} | {p[0]:.1f}→{p[1]:.1f} |')
    lines += ['', '코어/장면 시간은 호출 범위의 CPU 측정이며 지연된 Canvas 갱신·GPU 작업·전체 프레임 시간을 모두 포함하지 않는다. GC 세대별 카운터를 독립 수집 횟수로 합산하지 않는다. 3회 표본만으로 동등성이나 원인을 확정하지 않는다.']
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('before', 'after', 'output'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    args.output.write_text(report(args.before, args.after))
