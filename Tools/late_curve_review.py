"""Reproduce the 0.39 matched checkpoint report without replaying combat."""
import argparse
from statistics import median
from pathlib import Path
from regional_review import read, require, key, SEEDS, POLICIES

STAGES = (18, 19, 21, 22, 24, 25, 27, 28, 30, 31, 36, 42, 48, 54, 55, 57, 58, 60)


def report(inputs, before, after):
    expected = {(s, p, n) for s in STAGES for p in POLICIES for n in SEEDS}
    def index(rows, subset=False):
        result = {key(r): r for r in rows}
        require(len(result) == len(rows), 'duplicate row')
        require(expected <= set(result) if subset else expected == set(result), 'incomplete matrix')
        return result
    profiles = index(inputs['profiles']); old = index(before['rows'], True); new = index(after['rows'])
    for k in expected:
        for value in ('attack', 'health'):
            require(old[k][value] == new[k][value] == profiles[k]['snapshot'][value], 'equipment mismatch')
        require(old[k]['mission'] == new[k]['mission'], 'mission mismatch')
    lines = ['# 0.39 중반·일반 최종 작전 비교', '',
        f"build {before['build']} / 규칙 {before['rules']} → build {after['build']} / 규칙 {after['rules']}. 각 360판, 작전·조작별 10시드.",
        '기존 달력에서 실제 얻은 첫 진입 장비를 고정한 비교다. 새 경제 여정이나 사람의 평균 실력을 측정한 것은 아니다. 자연 조작은 회피가 서툰 조건을 점검하는 봇이다.', '',
        '| 지역 | 조작 | 승리 전→후 /40 | 저체력 시간 비율 중앙값 전→후 |',
        '|---|---|---:|---:|']
    for region, name in ((3, '냉각 시설'), (4, '중앙 공장'), (9, '궤도 조립항')):
        for policy in POLICIES:
            keys = sorted(k for k in expected if (k[0]-1)//6 == region and k[1] == policy)
            a, b = [new[k] for k in keys], [old[k] for k in keys]
            low = lambda rows: median(r['LowHealthSeconds']/max(1, r['Elapsed'])*100 for r in rows)
            lines.append(f"| {name} | {policy} | {sum(r['Won'] for r in b)}→{sum(r['Won'] for r in a)} | {low(b):.1f}%→{low(a):.1f}% |")
    lines += ['', '| 작전 | 조작 | 승리 전→후 /10 | 승→패 / 패→승 | 변경 후 패배: 보스 전 / 도달 후 |', '|---:|---|---:|---:|---:|']
    for stage in STAGES:
        for policy in POLICIES:
            keys = [(stage, policy, seed) for seed in SEEDS]
            a, b = [new[k] for k in keys], [old[k] for k in keys]
            lost = sum(old[k]['Won'] and not new[k]['Won'] for k in keys)
            gained = sum(not old[k]['Won'] and new[k]['Won'] for k in keys)
            lines.append(f"| {stage} | {policy} | {sum(r['Won'] for r in b)}→{sum(r['Won'] for r in a)} | {lost} / {gained} | {sum(not r['Won'] and not r['bossReached'] for r in a)} / {sum(not r['Won'] and r['bossReached'] for r in a)} |")
    for stage in (18, 31, 36, 42, 48, 54):
        require(all(old[k] == new[k] for k in expected if k[0] == stage), 'untouched neighbor changed')
    lines += ['', '변경 대상 밖 18·31·36·42·48·54작전 120판은 승패뿐 아니라 저장한 모든 전투 수치가 동일하다.',
        '냉각 시설의 자연 조작 저체력 비율은 감소하지 않았다. 승리 증가를 모든 체감 문제의 해결로 해석하지 않는다. 일반 60작전을 3~5승이라는 특정 숫자에 맞추기 위한 추가 조정은 하지 않았다.']
    lines += ['', '## 0.38 생존 패배 시점', '',
        '각 조건 10판 중 패배만 집계한다. 소수 표본의 분포이며 사람의 사망 시점으로 일반화하지 않는다. 기본 연습을 일괄 후반으로 옮기는 대신 각 계정의 실제 실패 시간대를 별도로 제공한다.', '',
        '| 작전 | 조작 | 패배 수 | 사망 초 최소 / 중앙 / 최대 |', '|---:|---|---:|---:|']
    for stage in (15, 21, 27):
        for policy in POLICIES:
            rows = [old[(stage, policy, seed)] for seed in SEEDS]
            times = [r['Elapsed'] for r in rows if not r['Won']]
            values = f'{min(times):.1f} / {median(times):.1f} / {max(times):.1f}' if times else '해당 없음'
            lines.append(f'| {stage} | {policy} | {len(times)} | {values} |')
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('inputs', 'before', 'after', 'output'): p.add_argument(name)
    args = p.parse_args()
    Path(args.output).write_text(report(read(args.inputs), read(args.before), read(args.after)))
