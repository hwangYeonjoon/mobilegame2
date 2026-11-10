"""Summarize matched operation/gear/seed trials; does not estimate human win rates."""
import argparse,gzip,json,hashlib,statistics
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('before',type=Path);p.add_argument('after',type=Path);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
def read(path):
 raw=gzip.decompress(path.read_bytes()) if path.suffix=='.gz' else path.read_bytes()
 return json.loads(raw),hashlib.sha256(raw).hexdigest()
b,bsha=read(a.before);n,nsha=read(a.after)
def key(r):return (r['seed'],r['policy'],r['gear'],r['stage'],r['mode'])
assert b['rules']==25 and n['rules']==26
assert len(b['rows'])==120 and len(n['rows'])==180
assert len({key(r) for r in b['rows']})==120 and len({key(r) for r in n['rows']})==180
bm={key(r):r for r in b['rows']};nm={key(r):r for r in n['rows'] if r['mode']=='campaign'}
assert bm.keys()==nm.keys()
for k in bm:
 assert (bm[k]['attack'],bm[k]['health'])==(nm[k]['attack'],nm[k]['health'])
lines=['# 0.35 외형 분리와 선택형 극한 작전 진단','','동일한 0.34 실제 장비 스냅샷과 시드를 사용했다. 실제 도달 장비(`earned`)와 대원 Lv.30·장비 Lv.20(`cap`)을 별도로 비교한다. 장비 종류·등급은 유지한다. 조건당 10시드 × 보스 3종이며 30명의 사람을 뜻하지 않는다. 수정 전 120판, 수정 후 180판.','','| 장비 | 조작 | 일반 수정 전 | 일반 수정 후 | 극한 |','|---|---|---:|---:|---:|']
for gear in ['earned','cap']:
 for policy in ['controlled_avoid','natural_legacy']:
  def score(data,mode):
   rows=[r for r in data['rows'] if r['gear']==gear and r['policy']==policy and r['mode']==mode]
   return f"{sum(r['Won'] for r in rows)}/{len(rows)}" if rows else '미측정'
  lines.append(f"| {gear} | {policy} | {score(b,'campaign')} | {score(n,'campaign')} | {score(n,'apex')} |")
lines+=['','| 상한 장비 · 작전 | 조작 | 일반 전 → 후 | 극한 |','|---|---|---:|---:|']
for stage in [48,54,60]:
 for policy in ['controlled_avoid','natural_legacy']:
  def wins(data,mode):
   rows=[r for r in data['rows'] if r['gear']=='cap' and r['policy']==policy and r['mode']==mode and r['stage']==stage]
   assert len(rows)==10
   return f"{sum(r['Won'] for r in rows)}/10"
  lines.append(f"| {stage} | {policy} | {wins(b,'campaign')} → {wins(n,'campaign')} | {wins(n,'apex')} |")
hard_natural=[r for r in n['rows'] if r['gear']=='cap' and r['mode']=='apex' and r['policy']=='natural_legacy']
lines+=['','일반 보스의 후반 후속 표적은 승률을 낮추지 않았다. 회피 봇 저체력 시간 비중은 약 1.86% → 1.88%로 거의 같아, 긴장감이 체감상 개선됐다고 단정하지 않는다. 일반 진행을 막는 벽을 추가하지 않고 고난도는 선택 모드로 분리했다.','',f'극한 자연 조작 {sum(r["Won"] for r in hard_natural)}/30은 실력 격차가 남아 있음을 보여 준다. 성장 상한 권장만으로 누구나 완료할 수 있다는 뜻이 아니다. 강한 회피 숙련이 필요하며, 파편·외형의 독점 경로가 아니다. 광고 부활·유료 입장·추가 성장 상한은 없다. 사람의 재미나 가격 수용성은 미검증이다.','',f'원본 SHA-256 (before): {bsha}',f'원본 SHA-256 (after): {nsha}','']
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text('\n'.join(lines));print('\n'.join(lines))
