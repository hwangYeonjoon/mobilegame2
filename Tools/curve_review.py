"""Summarize 0.38 paired difficulty and purchase-time evidence, without running battles."""
import argparse
from statistics import median
from pathlib import Path
from regional_review import read, require, key, SEEDS, POLICIES


def paid_report(inputs, before, after):
    expected = {(s, p, n) for s in (18, 24, 30, 36, 42, 48, 54, 60) for p in POLICIES for n in SEEDS}
    def index(rows):
        result = {key(r): r for r in rows}
        require(len(result) == len(rows) and set(result) == expected, 'incomplete/duplicate paid matrix')
        return result
    profiles, old, new = index(inputs['profiles']), index(before['rows']), index(after['rows'])
    for k in expected:
        for field in ('attack', 'health'):
            require(old[k][field] == new[k][field] == profiles[k]['snapshot'][field], 'paid gear mismatch')
    lines = ['# 구매 경로 장비의 전투 회귀 비교', '',
        f"변경 전 build {before['build']} / 규칙 {before['rules']}, 변경 후 build {after['build']} / 규칙 {after['rules']}. 각 160판.",
        '기존 28일 달력의 both_paid 경로에서 실제 첫 진입 장비를 복원했다. 각 경로 안에서만 같은 장비·시드로 전후 비교한다. 무료 경로와 도달 날짜가 다르므로 두 경로의 승률 차이를 현금의 인과 효과로 해석하지 않는다.', '',
        '| 작전 | 조작 | 승리 전→후 /10 | 피해 중앙값 전→후 | 저체력 시간 비율 중앙값 전→후 |',
        '|---:|---|---:|---:|---:|']
    for stage in sorted({k[0] for k in expected}):
        for policy in POLICIES:
            keys = [(stage, policy, seed) for seed in SEEDS]
            b, a = [old[k] for k in keys], [new[k] for k in keys]
            low = lambda rows: median(r['LowHealthSeconds'] / max(1, r['Elapsed']) * 100 for r in rows)
            lines.append(f"| {stage} | {policy} | {sum(r['Won'] for r in b)}→{sum(r['Won'] for r in a)} | {median(r['DamageReceived'] for r in b):.0f}→{median(r['DamageReceived'] for r in a):.0f} | {low(b):.1f}%→{low(a):.1f}% |")
    return '\n'.join(lines) + '\n'


def value_report(inputs, data):
    expected = {(s, 'natural_legacy', n) for s in range(18, 25) for n in SEEDS}
    profiles = {key(r): r for r in inputs['profiles']}
    require(len(profiles) == len(inputs['profiles']) and set(profiles) == expected, 'invalid value profiles')
    paths = ('free', 'cosmetic_only', 'paid_skip', 'paid_and_play')
    rows = {(key(r), r['path']): r for r in data['rows']}
    require(len(rows) == len(data['rows']) and set(rows) == {(k, p) for k in expected for p in paths}, 'incomplete/duplicate value matrix')
    for k in expected:
        free, cosmetic, paid = [rows[k, p] for p in paths[:3]]
        for p in paths:
            row = rows[k, p]
            for field in ('attack', 'health', 'heroLevel', 'equipped', 'wallet'):
                require(row['start'][field] == profiles[k]['snapshot'][field], 'value start mismatch')
            require(row['gemSpend'] == (100 if p.startswith('paid_') and row['purchaseUseful'] else 0), 'unexpected spend')
        for field in ('farmRuns', 'goalSeconds', 'won'):
            require(free[field] == cosmetic[field], 'cosmetics changed progression')
        if free['goalReached'] and paid['purchaseUseful']:
            require(free['entry']['equipped'] == paid['entry']['equipped'], 'same target gear differs')
            require(free['won'] == paid['won'], 'same gear battle differs')
    lines = ['# 18~24작전 무료 성장과 구매 시간 가치', '',
        f"build {data['build']} / 규칙 {data['rules']}. 7작전 × 10시드 × 4경로 = 280개 비교 경로. 각 경로의 반복 출격은 별도로 포함한다.",
        '같은 실제 진입 장비·지갑에서 출발한다. 무료는 보급 한 번으로 가능한 무기 레벨까지 최대 6회 반복한다. 구매 경로는 테스트 유료 보석 100개를 지급하고 1회 구매·강화한다. 실제 Google 결제·사람의 소요 시간·새 28일 경제 경로는 아니다.',
        '시간은 전투와 판당 가정한 메뉴 30초를 합산한다. 목표 미도달은 시간 0으로 바꾸지 않고 제외한다. 의상 경로는 기존 보석으로 600보석 의상을 산 뒤 무료와 같은 방식으로 성장한다.', '',
        '| 작전 | 무료 동일 장비 도달 /10 | 무료 반복 수 중앙값 | 구매가 생략한 시간 중앙값(분) | 목표 작전 승리: 무료 / 즉시 구매 / 구매+반복 |',
        '|---:|---:|---:|---:|---:|']
    for stage in range(18, 25):
        group = lambda path: [rows[(stage, 'natural_legacy', seed), path] for seed in SEEDS]
        free, paid, stacked = group('free'), group('paid_skip'), group('paid_and_play')
        times = [r['goalSeconds']/60 for r in free if r['goalReached'] and r['purchaseUseful']]
        time = f'{median(times):.1f}' if times else '측정 없음'
        lines.append(f"| {stage} | {sum(r['goalReached'] for r in free)} | {median(r['farmRuns'] for r in free):g} | {time} | {sum(r['won'] for r in free)} / {sum(r['won'] for r in paid)} / {sum(r['won'] for r in stacked)} |")
    battles = sum(sum(e['action'] == 'finish' for e in r['ledger']) for r in data['rows'])
    lines += ['', f'실제 정산한 측정 전투: {battles}판. 의상 구매 경로는 70개 모두 무료 경로와 반복 수·도달 시간·승패가 같다.',
        '구매 이득은 같은 성장에 이르는 반복을 생략하는 것이며 승리를 보장하지 않는다. 구매 후에도 같은 양을 플레이하는 경로는 추가 재료를 얻으므로 별도로 표시했다. 조건당 10시드로 사람의 평균 실력이나 구매 만족도를 추정하지 않는다.']
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('kind', choices=('paid', 'value')); p.add_argument('inputs'); p.add_argument('after'); p.add_argument('output'); p.add_argument('--before')
    args = p.parse_args()
    if args.kind == 'paid':
        require(bool(args.before), '--before required')
        report = paid_report(read(args.inputs), read(args.before), read(args.after))
    else:
        report = value_report(read(args.inputs), read(args.after))
    Path(args.output).write_text(report)
