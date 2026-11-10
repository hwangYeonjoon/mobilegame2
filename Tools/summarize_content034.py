"""Summarize the 0.34 continuation/action diagnostic, never human or causal revenue evidence."""
import argparse, gzip, hashlib, json
from pathlib import Path
from statistics import median

def summarize(data):
    assert data['rules'] == 25 and data['campaignCount'] == 60
    assert len(data['journeys']) == len(data['purchases']) == 20, 'incomplete diagnostic'
    rows=[]
    for policy in ('controlled_avoid','natural_legacy'):
        js=[j for j in data['journeys'] if j['policy']==policy]
        ps=[p for p in data['purchases'] if p['policy']==policy]
        assert len(js)==len(ps)==10 and len({j['seed'] for j in js})==10
        assert {j['seed'] for j in js}=={p['seed'] for p in ps}
        for j in js:
            stage=43
            for r in j['rows']:
                assert r['stage']==stage, 'skipped failed operation'
                if r['result']['Won']:stage+=1
            assert j['completed']==stage-1 and j['runs']==len(j['rows'])
        rows.append(dict(policy=policy,completed=sum(j['completed']==60 for j in js),runs=median(j['runs'] for j in js),
                         minutes=median(sum((r['result']['Elapsed']+30)/60 for r in j['rows']) for j in js),
                         attackPercent=median((p['after']['attack']/p['before']['attack']-1)*100 for p in ps),
                         winBefore=sum(p['beforeCombat']['Won'] for p in ps),winAfter=sum(p['afterCombat']['Won'] for p in ps),
                         gainWins=sum(not p['beforeCombat']['Won'] and p['afterCombat']['Won'] for p in ps),
                         lostWins=sum(p['beforeCombat']['Won'] and not p['afterCombat']['Won'] for p in ps)))
    return dict(rows=rows,battles=40+sum(j['runs'] for j in data['journeys']))

def main():
    p=argparse.ArgumentParser();p.add_argument('input',type=Path);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    raw=a.input.read_bytes()
    if a.input.suffix=='.gz':raw=gzip.decompress(raw)
    result=summarize(json.loads(raw));result['sha256']=hashlib.sha256(raw).hexdigest()
    lines=['# 0.34 구매 후 행동과 43~60작전 진단','',f"총 {result['battles']}판. 조건당 10시드. 실제 사람·결제 데이터가 아니다.",'',
           '| 조작 | 60작전 완료 | 확장 출격 중앙값 | 확장 활동 분 중앙값 | 구매 후 무기 강화 공격 증가 중앙값 | 같은 작전 구매 전/후 승리 | 승리→패배 |','|---|---:|---:|---:|---:|---:|---:|']
    for r in result['rows']:lines.append(f"| {r['policy']} | {r['completed']}/10 | {r['runs']:g} | {r['minutes']:.1f} | {r['attackPercent']:.1f}% | {r['winBefore']}/10 → {r['winAfter']}/10 | {r['lostWins']} |")
    lines += ['', '구매 비교는 기존 보유 재료까지 사용해 무기를 강화한 경로와 강화하지 않은 동일 스냅샷의 비교다. 무료 경로도 최적으로 강화한 대조가 아니므로 공격 증가 전체를 현금의 순수 효과로 해석할 수 없다. 공격 수치가 증가해도 봇 이동·선택·적 처치 경로가 바뀌므로 모든 시드의 승리가 보장되지는 않는다.', '',
              '확장은 0.33에서 실제로 획득한 첫 42완료일 장비를 출발점으로 한다. 하루 6출격, 작전당 최대 6시도, 일반 진행 중 무료 현장 보급, 실제 보상과 균형 강화. 광고·성장 구매·보급을 위한 강제 반복은 없다. 60작전 신규 계정 전체 달력이나 장기 매출 예측은 아니다.', '', '원본 SHA-256: '+result['sha256']]
    a.output.write_text('\n'.join(lines)+'\n');a.output.with_suffix('.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')

if __name__=='__main__':main()
