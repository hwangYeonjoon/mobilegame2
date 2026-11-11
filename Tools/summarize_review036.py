"""Reproduce the 0.36 review without rerunning combat; not a human or revenue study."""
import argparse,gzip,json,hashlib
from collections import defaultdict,Counter
from pathlib import Path
from statistics import median
from summarize_long_audit import summarize,completion_minutes,need

def read(path):
    raw=Path(path).read_bytes()
    if str(path).endswith('.gz'):raw=gzip.decompress(raw)
    return json.loads(raw),hashlib.sha256(raw).hexdigest()
def med(values):return round(median(values),1) if values else None
def main():
    p=argparse.ArgumentParser();p.add_argument('calendar');p.add_argument('mid');p.add_argument('output');a=p.parse_args()
    d,sha=read(a.calendar);v,vsha=read(a.mid)
    need((d['build'],d['rules'],v['build'],v['rules'])==(39,26,39,26),'must use pre-regional 0.36 evidence')
    checked=summarize(d);groups=defaultdict(list)
    for j in d['journeys']:groups[(j['policy'],j['cohort'])].append(j)
    lines=['# 0.36 무료 진행·시간 구매 재검증','',f"전투 규칙 26 / build 39. {checked['journeys']}경로, {checked['battles']}판, 28일·하루 최대 6판, 조건당 10시드. 0.37 몬스터 변경 전 결과다.",
        '', '출석·임무·현장 보급·무료 패스와 주간 보상은 실제 정산 코드로 수령한다. 광고 수입은 없다. 현장 보급은 일반 전투 300초 합산이며 별도 반복 3승 의무가 아니다. 구매 경로도 같은 하루 출격 예산을 사용한다.',
        '', '| 조작 | 경로 | 60 완료 | 완료 날짜 중앙값 | 완료까지 활동 분 | 장착 성장 상한 날짜 | 보급 횟수 |', '|---|---|---:|---:|---:|---:|---:|']
    labels={'reserve':'보석 보존','cosmetic_free':'무료 보석으로 의상만','kit_free':'무료 보석으로 보급','both_paid':'보급+패스, 부족분 모의 충전'}
    for (policy,cohort),js in sorted(groups.items()):
        capped=[next((x['day'] for x in j['days'] if x['state']['heroLevel']>=30 and len(x['state']['equipped'])==3 and all(g['level']>=20 for g in x['state']['equipped'])),None) for j in js]
        days=[j['firstCampaignDay'] for j in js if j['firstCampaignDay']]
        minutes=[completion_minutes(j,60) for j in js if j['firstCampaignDay']]
        lines.append(f"| {policy} | {labels[cohort]} | {len(days)}/{len(js)} | {med(days)} | {med(minutes)} | {med([x for x in capped if x])} | {med([sum(e['action']=='buy_kit' for e in j['ledger']) for j in js])} |")
    lines+=['','## 같은 시드에서 줄어든 시간','','양쪽 모두 60작전을 완료한 짝만 비교한다. 양수는 구매 경로가 빠름, 음수는 느림. 활동 시간은 전투와 판당 메뉴 30초를 합한 가정이다. 완료 이후 반복은 제외했다.','','| 조작 | 비교 경로 | 날짜 절약 중앙값 [범위] | 활동 분 절약 중앙값 [범위] |','|---|---|---:|---:|']
    for policy in d['policies']:
        base={j['seed']:j for j in groups[(policy,'reserve')]}
        for cohort in ('cosmetic_free','kit_free','both_paid'):
            js=groups[(policy,cohort)];days=[];minutes=[]
            for j in js:
                b=base[j['seed']]
                if b['firstCampaignDay'] and j['firstCampaignDay']:
                    days.append(b['firstCampaignDay']-j['firstCampaignDay']);minutes.append(completion_minutes(b,60)-completion_minutes(j,60))
                if cohort=='cosmetic_free':
                    need([(r['stage'],r['won'],r['seconds']) for r in b['runs']]==[(r['stage'],r['won'],r['seconds']) for r in j['runs']],'cosmetic combat divergence')
            def spread(xs):return '—' if not xs else f'{med(xs)} [{min(xs):.1f}–{max(xs):.1f}]'
            lines.append(f'| {policy} | {labels[cohort]} | {spread(days)} | {spread(minutes)} |')
    lines+=['','## 18~24작전: 같은 무기 레벨까지 무료 반복 vs 보급 1회','','현재 0.36 무료 경로에서 획득한 첫 진입 장비·잔액, 자연 조작 10시드. 구매 경로에 100 테스트 유료 보석을 지급하지만 지출은 무료 보석 우선이다. 현금 100보석 구매를 관찰한 것이 아니다. 무료는 최대 6회 반복하고 현장 보급 1회를 받을 수 있다. 구매+플레이 경로는 무료와 같은 횟수만큼 더 플레이한다. 다른 슬롯 강화·광고·출석·달력 변화는 제외했다.',
        '', '| 작전 | 구매 추가 무기 단계 | 무료 목표 도달 | 생략 가능한 분 | 무료/즉시구매 승리 | 무료/구매+같은 반복 공격력 |','|---|---:|---:|---:|---:|---:|']
    indexed={}
    for r in v['rows']:
        key=(r['stage'],r['seed'],r['path']);need(key not in indexed,'duplicate checkpoint');indexed[key]=r
        need(r['policy']=='natural_legacy','unexpected checkpoint policy')
        for e in r['ledger']:need({k:e['before'][k]+e['delta'][k] for k in e['before']}==e['after'],'checkpoint wallet delta')
    need(len(indexed)==280,'70 profiles x 4 paths')
    for stage in range(18,25):
        free=[r for r in v['rows'] if r['stage']==stage and r['path']=='free'];need(len(free)==10,'missing seeds')
        skip=[indexed[(stage,r['seed'],'paid_skip')] for r in free];stack=[indexed[(stage,r['seed'],'paid_and_play')] for r in free]
        for f,b in zip(free,skip):
            cosmetic=indexed[(stage,f['seed'],'cosmetic_only')]
            need((f['entry']['attack'],f['won'],f['goalSeconds'])==(cosmetic['entry']['attack'],cosmetic['won'],cosmetic['goalSeconds']),'cosmetic changed progress')
            need(f['targetLevel']==b['targetLevel'] and b['farmRuns']==0,'unequal goals')
        minutes=[f['goalSeconds']/60 for f,b in zip(free,skip) if f['goalReached'] and b['purchaseUseful']]
        lines.append(f"| {stage} | {med([r['extraLevels'] for r in skip])} | {sum(r['goalReached'] for r in free)}/10 | {med(minutes)} | {sum(r['won'] for r in free)}/10 · {sum(r['won'] for r in skip)}/10 | {med([r['entry']['attack'] for r in free])} / {med([r['entry']['attack'] for r in stack])} |")
    tiers=Counter(e['target'] for j in d['journeys'] for e in j['ledger'] if e['action']=='buy_kit')
    lines+=['','## 판단과 한계','',
        '- 무료 보석을 보존하거나 의상에 사용해도 모든 표본이 60작전을 완료했다. 구매가 진행 필수라는 신호는 이 조건에서 확인되지 않았다. 무료·유료 보급을 같은 분량으로 바꾸지는 않는다.',
        '- 자연 조작에서는 보급으로 반복 시간을 줄이고 같은 시간을 플레이하면 추가 성장이 가능하다. 같은 목표 장비를 갖춘 무료/즉시 구매 경로의 결과가 같다는 것은 시간 구매 의도와 맞는다. 구매 직후 무조건 승리한다는 뜻은 아니다.',
        '- 유료 경로는 무료 보석 우선 소비, 필요할 때 제안가 7,500원에 해당하는 1,200보석 모의 충전을 했다. 실제 결제·매출·구매 만족도 자료가 아니다. 패스와 보급의 효과를 각각 분리한 인과 추정도 아니다.',
        '- 장착 성장 상한에 도달하면 구매가 중단된다. 모든 경로에서 마지막 7일 보급 구매는 0회다. 대체 대원·장비와 장기 반복 구매 수요는 입증되지 않았다. 성장 상한을 억지로 늘리거나 구매 효과가 없는 보급을 팔지 않는다.',
        '- 0.37의 지역 몬스터 변경은 별도 전후 진단이다. 이 달력 측정을 변경 후 전체 승률로 재사용하지 않는다. 실기기 성능·사람의 재미·과금 압박 체감은 미검증이다.',
        f'- 실제 모의 구매 수: {dict(tiers)}. 심부 보급은 이번 전체 장비 균형 강화 경로에서 사용됐다. 무기만 보는 기존 31작전 표본의 수요 0을 모든 성장 대상으로 일반화할 수 없다.',
        '- 중간 구간 원본의 고정 assumptions 문장에 “Three checkpoints”가 남아 있으나 실제 입력/출력은 18~24작전 7곳, 70프로필/280경로다. 원본은 수정하지 않았고 생성 도구의 이후 메타데이터만 일반화했다.',
        '',f'달력 SHA-256: `{sha}`',f'중간 구간 SHA-256: `{vsha}`']
    Path(a.output).write_text('\n'.join(lines)+'\n')
    print('Validated 80 calendar journeys and 70 paired mid-campaign profiles')
if __name__=='__main__':main()
