"""Matched regional regression: earned gear, fixed seeds, no human-difficulty claims."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
from statistics import median

POLICIES = ('natural_legacy', 'controlled_avoid')
SEEDS = tuple(17 + 37 * i for i in range(10))
STAGES = tuple(r * 6 + n for r in range(10) for n in (1, 3, 4, 6))
REGIONS = ('고철 정비소', '폐철도', '오염 처리장', '냉각 시설', '중앙 공장', '고압 발전소', '심층 용광로', '자력 선별장', '폭풍 송전탑', '궤도 조립항')

def require(ok, message):
    if not ok:
        raise ValueError(message)

def read(path):
    raw = Path(path).read_bytes()
    return json.loads(gzip.decompress(raw) if str(path).endswith('.gz') else raw)

def key(row):
    return row['stage'], row['policy'], row['seed']

def indexed(rows):
    result = {key(r): r for r in rows}
    require(len(result) == len(rows), 'duplicate stage/policy/seed')
    require(set(result) == {(s, p, n) for s in STAGES for p in POLICIES for n in SEEDS}, 'incomplete 800-run matrix')
    return result

def make_inputs(calendar):
    profiles = []
    for journey in calendar['journeys']:
        if journey['cohort'] != 'reserve':
            continue
        for stage in STAGES:
            run = next((r for r in journey['runs'] if r['stage'] == stage and not r['repeat'] and not r['mode']), None)
            require(run is not None, f'unreached stage {stage}; never invent equipment')
            profiles.append(dict(seed=journey['seed'], stage=stage, policy=journey['policy'], snapshot=run['before']))
    indexed(profiles)
    return dict(source='docs/reports/review036-calendar.json.gz', note='Reserve first-entry earned equipment; four operations per region; ten seeds; two policies. Matched snapshots, not new economic trajectories.', profiles=sorted(profiles, key=key))

def report(inputs, before, after):
    profiles = indexed(inputs['profiles']); b = indexed(before['rows']); a = indexed(after['rows'])
    for k, row in a.items():
        old = b[k]; snap = profiles[k]['snapshot']
        for value in ('attack', 'health'):
            require(row[value] == old[value] == snap[value], f'gear mismatch {k} {value}')
        require(row['mission'] == old['mission'], f'mission mismatch {k}')
        for r in (row, old):
            require(abs(sum(r['damage']) - r['DamageReceived']) < max(1, r['DamageReceived'] * .001), 'damage source sum mismatch')
            require(len(r['damage']) == 6 and len(r['healing']) == 7, 'unknown source schema')
    lines = ['# 지역 몬스터 확대 회귀 측정', '',
        f"변경 전 build {before['build']} / 규칙 {before['rules']}, 변경 후 build {after['build']} / 규칙 {after['rules']}. 각각 800판, 총 1,600판.",
        '10지역 × 지역당 진입·생존·중간 보스·최종 작전 4곳 × 두 조작 × 10시드. 보석 보존 경로의 실제 첫 진입 장비를 복원하고 공격력·체력을 대조했다.',
        '동일 장비·동일 시드의 지역 행동 변경 비교이며, 새 규칙으로 28일 경제 경로 전체를 다시 진행한 자료는 아니다. 자연 조작은 사람의 평균 실력이 아니다.', '',
        '| 지역 | 조작 | 승리 전→후 /40 | 승→패 / 패→승 | 피해 중앙값 전→후 | 저체력 시간 비율 중앙값 전→후 |',
        '|---|---|---:|---:|---:|---:|']
    med = lambda rows, field: median(r[field] for r in rows)
    for region, name in enumerate(REGIONS):
        for policy in POLICIES:
            keys = sorted(k for k in a if (k[0]-1)//6 == region and k[1] == policy)
            old = [b[k] for k in keys]; new = [a[k] for k in keys]
            lost = sum(b[k]['Won'] and not a[k]['Won'] for k in keys)
            gained = sum(not b[k]['Won'] and a[k]['Won'] for k in keys)
            low = lambda rows: median(r['LowHealthSeconds']/max(1, r['Elapsed'])*100 for r in rows)
            lines.append(f"| {name} | {policy} | {sum(r['Won'] for r in old)}→{sum(r['Won'] for r in new)} | {lost} / {gained} | {med(old,'DamageReceived'):.0f}→{med(new,'DamageReceived'):.0f} | {low(old):.1f}%→{low(new):.1f}% |")
    lines += ['', '## 작전·목표별 결과', '', '피해 비중은 변경 후 받은 총피해를 합산한 비율이며, 개별 판 비율의 중앙값이 아니다. 기타는 미분류와 조건 피해다. 패배는 보스 도달 여부로 구분한다.', '',
        '| 작전 | 목표 | 조작 | 승리 전→후 /10 | 후 패배: 보스 전 / 보스 도달 | 후 피해: 접촉 / 적 공격 / 지형 / 보스 / 기타 % |',
        '|---:|---|---|---:|---:|---|']
    for stage in STAGES:
        for policy in POLICIES:
            keys = [(stage, policy, seed) for seed in SEEDS]; rows = [a[k] for k in keys]
            total = sum(r['DamageReceived'] for r in rows)
            damage = [sum(r['damage'][i] for r in rows) for i in range(6)]
            shares = ' / '.join(f'{v/max(1,total)*100:.0f}' for v in (damage[1],damage[2],damage[3],damage[5],damage[0]+damage[4]))
            deaths = [r for r in rows if not r['Won']]
            lines.append(f"| {stage} | {rows[0]['mission']} | {policy} | {sum(b[k]['Won'] for k in keys)}→{sum(a[k]['Won'] for k in keys)} | {sum(not r['bossReached'] for r in deaths)} / {sum(r['bossReached'] for r in deaths)} | {shares} |")
    lines += ['', '## 해석 범위', '',
        '- 각 작전·조작은 10시드다. 1~2승 차이를 확정적인 난이도 변화로 해석하지 않는다. 지역 합계도 서로 다른 목표를 섞으므로 상세 표와 함께 읽는다.',
        '- 예고 중 멈추는 적은 추격 압박이 줄 수 있다. 전체 승률을 일정하게 만들려고 모든 적 체력·공격력을 일괄 올리지 않는다.',
        '- 고정 Lv.5 장비, 인위적으로 주입한 밀집 화면, 무료/유료 달력 결과는 별도 자료다. 이 표를 사람의 승률·구매 전환·실물 폰 성능으로 사용하지 않는다.']
    return '\n'.join(lines) + '\n'

def main():
    p = argparse.ArgumentParser(); sub = p.add_subparsers(dest='command', required=True)
    build = sub.add_parser('inputs'); build.add_argument('calendar'); build.add_argument('output')
    compare = sub.add_parser('report')
    for name in ('inputs', 'before', 'after', 'output'): compare.add_argument(name)
    args = p.parse_args()
    if args.command == 'inputs':
        Path(args.output).write_text(json.dumps(make_inputs(read(args.calendar)), ensure_ascii=False))
    else:
        text = report(read(args.inputs), read(args.before), read(args.after))
        for name in ('inputs', 'before', 'after'):
            path = Path(getattr(args, name)); text += f'\n{path.name} SHA-256: `{hashlib.sha256(path.read_bytes()).hexdigest()}`\n'
        Path(args.output).write_text(text)

if __name__ == '__main__':
    main()
