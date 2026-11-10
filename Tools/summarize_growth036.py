"""Reproduce paired checkpoint time-value diagnostics; no human or revenue estimates."""
import argparse,gzip,json,statistics,hashlib
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('input',type=Path);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
raw=gzip.decompress(a.input.read_bytes()) if a.input.suffix=='.gz' else a.input.read_bytes();d=json.loads(raw);rows=d['rows']
assert d['build']==39 and d['rules']==26 and len(rows)==240
keys={(r['stage'],r['policy'],r['seed'],r['path']) for r in rows};assert len(keys)==240
for r in rows:
 wallet=dict(r['start']['wallet'])
 for e in r['ledger']:
  assert wallet==e['before']
  for c in wallet:assert e['after'][c]-wallet[c]==e['delta'][c]
  wallet=e['after']
 assert wallet==r['final']['wallet']
 if r['path'].startswith('paid_'):assert r['gemSpend']==(100 if r['purchaseUseful'] else 0)
 assert r['goalReached']==(r['goalSeconds']>=0)
by={(r['stage'],r['policy'],r['seed'],r['path']):r for r in rows}
for r in rows:
 if r['path']!='free':continue
 c=by[(r['stage'],r['policy'],r['seed'],'cosmetic_only')]
 assert (r['farmRuns'],r['goalSeconds'],r['entry']['attack'],r['won'])==(c['farmRuns'],c['goalSeconds'],c['entry']['attack'],c['won'])
def med(v):return f'{statistics.median(v):.1f}' if v else '해당 없음'
lines=['# 0.36 성장 보급 시간 가치 측정','','0.33 보석 보존 경로의 실제 획득 장비·재화 스냅샷에서 시작했다. 처음부터 다시 진행한 경제 예측은 아니다. 무기 경로 하나에 한정한 7·13·31작전 진입 비교이며 조건당 시드 10개다. 구매 금액은 100보석이며 지급한 테스트 보석은 실제 현금 결제가 아니다.','','| 작전 | 조작 | 구매로 추가 단계 중앙값 | 유효 구매 표본 | 무료 목표 도달 | 무료 반복 횟수 중앙값 | 구매 생략의 시간 절약 중앙값 |','|---|---|---:|---:|---:|---:|---:|']
for stage in [7,13,31]:
 for policy in ['controlled_avoid','natural_legacy']:
  free=[r for r in rows if r['stage']==stage and r['policy']==policy and r['path']=='free'];valid=[r for r in free if r['purchaseUseful']]
  saved=[]
  for r in valid:
   paid=by[(stage,policy,r['seed'],'paid_skip')]
   if r['goalReached'] and paid['goalReached']:saved.append((r['goalSeconds']-paid['goalSeconds'])/60)
  lines.append(f'| {stage} | {policy} | {med([r["extraLevels"] for r in free])} | {len(valid)}/10 | {sum(r["goalReached"] for r in valid)}/{len(valid)} | {med([r["farmRuns"] for r in valid])} | {(med(saved)+"분") if saved else "해당 없음"} ({len(saved)}쌍) |')
lines+=['','| 작전 | 조작 | 무료: 진입 공격·승리 | 구매 후 반복 생략 | 구매 + 같은 횟수 반복 |','|---|---|---|---|---|']
for stage in [7,13,31]:
 for policy in ['controlled_avoid','natural_legacy']:
  def stat(path):
   rr=[r for r in rows if r['stage']==stage and r['policy']==policy and r['path']==path]
   return f'{med([r["entry"]["attack"] for r in rr])} · {sum(r["won"] for r in rr)}/10'
  lines.append(f'| {stage} | {policy} | {stat("free")} | {stat("paid_skip")} | {stat("paid_and_play")} |')
lines+=['','무료와 외형 구매 경로는 60쌍 모두 목표 도달 시간·강화 능력치·후속 작전 결과가 같다. 600보석 미만이면 외형 구매를 건너뛰므로 전원이 외형을 샀다는 뜻은 아니다.','',
'무료 경로는 구매 1회로 가능한 무기 레벨까지 최근 완료 작전을 최대 6회 반복한다. 실제 실패 보상과 무료 현장 보급을 정산한다. 구매 경로는 이 반복을 생략하며, 추가 반복 경로는 무료와 같은 횟수만큼 싸운 후 더 강화한다. 마지막에 모두 같은 시드의 목표 작전을 한 번 시도한다. 시간은 전투 모의 시간 + 판당 메뉴 30초 가정이다. 목표 미달은 시간 절약 통계에서 제외하며 도달 수를 함께 표시한다.','',
'각 경로는 같은 날, 같은 초기 장비·재화다. 출석·임무·광고·패스·장비 교체·다른 슬롯 강화는 제외했다. 무료 보급은 미수령한 새 날로 가정했다. 이미 수령한 날이나 자연 진행 중 얻는 보상까지 포함한 전체 캠페인 시간 절약으로 일반화하지 않는다. 유료 보석을 100개 추가해도 실제 차감은 기존 무료 보석 우선 정책을 따른다. 보석 구매의 효과를 비교하는 실험이지 현금 매출 예측이 아니다.','',
'31작전 무기 상한 표본은 새 심부 보급의 판매 수요를 증명하지 않는다. 대체 대원·장비를 키우려는 사람의 수요는 별도 검증 대상이다. 보급 확대만으로 장기 성장 수요가 생겼다고 주장하지 않는다. 게임 내 절약 시간 표시는 아직 제공하지 않는다.','',f'실제 모의 전투: {sum(r["farmRuns"]+1 for r in rows)}판 · 원본 SHA-256: {hashlib.sha256(raw).hexdigest()}','']
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text('\n'.join(lines));print('\n'.join(lines))
