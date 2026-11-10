"""Validate and summarize the real-economy 0.33 calendar simulation; never human evidence."""
import argparse
from collections import Counter,defaultdict
import gzip,hashlib,json
from pathlib import Path
from statistics import median
from datetime import date as Date,timedelta

COHORTS={'reserve','cosmetic_free','kit_free','both_paid'}
PACKS={'gems_300':(300,2200),'gems_600':(600,3900),'gems_1200':(1200,7500),'gems_2600':(2600,14900)}
def need(b,msg):
    if not b:raise ValueError(msg)
def stat(xs):return None if not xs else dict(median=median(xs),min=min(xs),max=max(xs))
def completion_minutes(j):
    total=0
    for r in j['runs']:
        total+=(r['seconds']+30)/60
        if not r['mode'] and not r['repeat'] and r['stage']==42 and r['won']:return total
    return None
def last_week(j,days):
    if days<8:return None
    start,end=j['days'][-8],j['days'][-1]
    cutoff=(Date.fromisoformat(j['ledger'][0]['serviceDate'])+timedelta(days=days-7)).isoformat()
    return dict(freeChange=(end['state']['wallet']['gems']-end['paid'])-(start['state']['wallet']['gems']-start['paid']),kits=sum(x['action']=='buy_kit' and x['serviceDate']>=cutoff for x in j['ledger']),completedBefore=start['cleared']==42)
def fmt(s):return '—' if s is None else f"{s['median']:.1f} [{s['min']:.1f}–{s['max']:.1f}]"
def summarize(d):
    need(d['kind']=='long_horizon_not_human' and d['schema']==1,'schema')
    groups=defaultdict(list);seen=set()
    for j in d['journeys']:
        key=(j['cohort'],j['policy'],j['seed']);need(key not in seen,'duplicate journey');seen.add(key);groups[key[:2]].append(j)
        need(key[0] in COHORTS and key[1] in ('natural_legacy','controlled_avoid'),'cohort/policy')
        wallet={'gold':1800,'parts':30,'gems':0};paid=pin=pout=fin=fout=won=0;counts=Counter();claims=set();started=None;ri=0;cleared=set();daily_wins=Counter();weekly_scores={};robot_eligible=set();owned_pass=False;end_wallets={}
        for seq,x in enumerate(j['ledger'],1):
            need(x['sequence']==seq and x['before']==wallet and x['paidBefore']==paid,'wallet continuity')
            after={k:wallet[k]+x['delta'][k] for k in wallet};need(after==x['after'] and min(after.values())>=0,'delta')
            dp=x['paidAfter']-paid;df=x['delta']['gems']-dp;a=x['action'];date=x['serviceDate'];counts[(date,a)]+=1
            need(a in {'attendance','missions','salvage_claim','weekly_claim','robot_week_claim','pass_free_claim','pass_premium_claim','buy_pass','buy_kit','cosmetic_buy','simulated_topup','upgrade','transfer','equip','battle_start','battle_finish'},'unknown source')
            need(0<=x['paidAfter']<=after['gems'],'paid provenance')
            if a=='simulated_topup':
                need(j['cohort']=='both_paid' and x['target'] in PACKS,'illegal funding');g,p=PACKS[x['target']]
                need(dp==g and x['delta']==dict(gold=0,parts=0,gems=g),'pack grant');won+=p
            else:need(dp<=0,'paid mint')
            if x['delta']['gems']<0:need(x['paidAfter']==paid-max(0,-x['delta']['gems']-(wallet['gems']-paid)),'free-first')
            if a in ('salvage_claim','buy_kit','attendance','missions'):need(counts[(date,a)]<=1,'daily cap')
            if a=='salvage_claim':need(daily_wins[date]>=3 and x['delta']==dict(gold=4500,parts=40,gems=0),'unearned salvage')
            if a=='buy_kit':need(j['cohort'] in ('kit_free','both_paid') and x['delta']==dict(gold=4500,parts=40,gems=-100),'kit')
            if a=='buy_pass':need(j['cohort']=='both_paid' and x['delta']==dict(gold=0,parts=0,gems=-900),'pass');owned_pass=True
            if a=='cosmetic_buy':need(j['cohort']=='cosmetic_free' and x['delta']==dict(gold=0,parts=0,gems=-600),'cosmetic')
            if a in ('pass_free_claim','pass_premium_claim'):
                t=int(x['target']);premium=a=='pass_premium_claim';need(0<=t<4 and (t+1)*3 in cleared and (not premium or owned_pass),'unearned pass')
                need(x['delta']==dict(gold=3000+1000*t if premium else 1000+500*t,parts=30+10*t if premium else 10+5*t,gems=0),'pass reward')
            if a=='weekly_claim':
                week,t=x['target'].rsplit(':',1);t=int(t);need(0<=t<4 and weekly_scores.get(week,-1)>=[0,5000,15000,30000][t],'unearned weekly')
                need(x['delta']==dict(gold=[1000,1000,1500,2000][t],parts=[10,20,30,40][t],gems=50 if t==3 else 0),'weekly reward')
            if a=='robot_week_claim':need(x['target'] in robot_eligible and x['delta']==dict(gold=3000,parts=30,gems=0),'unearned robot reward')
            if a=='upgrade':
                slot=int(x['target'].split(':')[0]);level=int(x['target'].rsplit(':',1)[1]);need(x['delta']==dict(gold=-level*(500 if slot==3 else 300),parts=-level*(3 if slot==3 else 2),gems=0),'upgrade cost')
            if a in ('transfer','equip','battle_start'):need(not any(x['delta'].values()),'free equipment action changed wallet')
            if a.endswith('_claim') or a in ('buy_pass','cosmetic_buy'):
                identity=(a,date if a=='salvage_claim' else x['target']);need(identity not in claims,'duplicate reward');claims.add(identity)
            if a=='battle_start':
                need(started is None and ri<len(j['runs']),'start');r=j['runs'][ri];need(r['before']['wallet']==wallet and set(r['before']['clearedStages'])==cleared,'battle entry')
                need(r['mode']=='weekly' or (r['stage'] in cleared if r['repeat'] else r['stage']==len(cleared)+1),'future progression');started=x['target']
            if a=='battle_finish':
                need(started is not None and started==x['target'],'settlement');r=j['runs'][ri];need(r['after']['wallet']==after,'receipt')
                if not r['mode']:
                    need(x['delta']['gems']==(50 if r['won'] and not r['repeat'] else 0),'first-clear gems')
                    if r['won']:
                        if r['repeat'] and r['seconds']>=90:daily_wins[date]+=1
                        cleared.add(r['stage'])
                else:
                    need(x['delta']==dict(gold=0,parts=0,gems=0),'weekly direct payout')
                    week=(Date(2026,1,1)+timedelta(days=r['seed']-40000)).isoformat()
                    if r['seconds']>=30:weekly_scores[week]=max(weekly_scores.get(week,0),r['score'])
                    # Necessary eligibility check. Rank/frame proof is tested through authoritative replay separately.
                    if r['seconds']>=60 and r['machines']>0:robot_eligible.add(week)
                need(set(r['after']['clearedStages'])==cleared,'post battle progression');ri+=1;started=None
            pin+=max(0,dp);pout+=max(0,-dp);fin+=max(0,df);fout+=max(0,-df);paid=x['paidAfter'];wallet=after;end_wallets[date]=(dict(wallet),paid)
        need(started is None and ri==len(j['runs']),'unsettled')
        need((pin,pout,fin,fout,paid,won)==tuple(j[k] for k in ('paidGranted','paidSpent','freeEarned','freeSpent','paidLeft','proposedWon')),'totals')
        need(len(j['days'])==d['days'] and j['days'][-1]['state']['wallet']==wallet,'calendar/final')
        need(abs(sum((r['seconds']+30)/60 for r in j['runs'])-j['minutes'])<.2,'time')
        for day,x in enumerate(j['days'],1):
            day_date=(Date.fromisoformat(j['ledger'][0]['serviceDate'])+timedelta(days=day-1)).isoformat();need(end_wallets.get(day_date)==(x['state']['wallet'],x['paid']),'daily wallet snapshot')
            rs=[r for r in j['runs'] if r['day']==day];need(x['day']==day and len(rs)<=d['slots'] and [r['slot'] for r in rs]==list(range(1,len(rs)+1)),'slots')
            past=[r for r in j['runs'] if r['day']<=day];need(x['cleared']==sum(not r['mode'] and not r['repeat'] and r['won'] for r in past),'checkpoint progression')
        completion=next((r['day'] for r in j['runs'] if not r['mode'] and not r['repeat'] and r['stage']==42 and r['won']),0)
        need(completion==j['first42Day'],'completion/censoring')
    need(len(groups)==8,'missing cohort')
    need(len({tuple(sorted(j['seed'] for j in js)) for js in groups.values()})==1 and all(len(js)==d['seeds'] for js in groups.values()),'unpaired seeds')
    rows=[];pairs=[]
    for (c,p),js in sorted(groups.items()):
        rows.append(dict(cohort=c,policy=p,n=len(js),clear42=sum(j['first42Day']>0 for j in js),cleared=stat([j['days'][-1]['cleared'] for j in js]),day42=stat([j['first42Day'] for j in js if j['first42Day']]),minutes=stat([j['minutes'] for j in js]),
            completionMinutes=stat([completion_minutes(j) for j in js if j['first42Day']]),postCompletionMinutes=stat([j['minutes']-completion_minutes(j) for j in js if j['first42Day']]),won=stat([j['proposedWon'] for j in js]),paidSpent=stat([j['paidSpent'] for j in js]),freeLeft=stat([j['days'][-1]['state']['wallet']['gems']-j['paidLeft'] for j in js]),
            lastWeekFreeChange=stat([last_week(j,d['days'])['freeChange'] for j in js if d['days']>=8]),lastWeekKits=stat([last_week(j,d['days'])['kits'] for j in js if d['days']>=8]),completedBeforeLastWeek=sum(last_week(j,d['days'])['completedBefore'] for j in js if d['days']>=8),equippedGrowthCapped=sum(j['days'][-1]['state']['heroLevel']==30 and all(g['level']==20 for g in j['days'][-1]['state']['equipped']) for j in js),cosmeticOwners=sum(any(x['action']=='cosmetic_buy' for x in j['ledger']) for j in js),salvageClaims=stat([sum(x['action']=='salvage_claim' for x in j['ledger']) for j in js]),
            weeks=stat([sum(x['action']=='robot_week_claim' for x in j['ledger']) for j in js]),postCampaignRuns=stat([sum(r['day']>j['first42Day'] for r in j['runs']) for j in js if j['first42Day']]),
            firstClearGems=stat([sum(x['delta']['gems'] for x in j['ledger'] if x['action']=='battle_finish') for j in js]),recurringGems=stat([sum(x['delta']['gems'] for x in j['ledger'] if x['action'] in ('attendance','missions','weekly_claim')) for j in js])))
        if c!='reserve':
            base={j['seed']:j for j in groups[('reserve',p)]}
            for j in js:
                b=base[j['seed']];pairs.append(dict(cohort=c,policy=p,seed=j['seed'],stageDifference=j['days'][-1]['cleared']-b['days'][-1]['cleared'],daysSaved=b['first42Day']-j['first42Day'] if b['first42Day'] and j['first42Day'] else None))
    return dict(rows=rows,pairs=pairs,journeys=len(seen),battles=sum(len(j['runs']) for j in d['journeys']),days=d['days'],seeds=d['seeds'])
def main():
    p=argparse.ArgumentParser();p.add_argument('input');p.add_argument('--json',required=True);p.add_argument('--markdown',required=True);a=p.parse_args();raw=Path(a.input).read_bytes()
    if a.input.endswith('.gz'):raw=gzip.decompress(raw)
    d=json.loads(raw);s=summarize(d);s['sha256']=hashlib.sha256(raw).hexdigest();Path(a.json).write_text(json.dumps(s,ensure_ascii=False,indent=2)+'\n')
    lines=['# 0.33 28일 성장·구매 관찰','',f"{s['journeys']}경로 / {s['battles']}판 · 조건당 시드 {s['seeds']}개 · {s['days']}일. 사람 결과가 아님.",'','| 조작 | 경로 | 42 완료 | 최종 작전 | 완료 일수(도달자만) | 전체 활동 분 | 충전 제안가 | 유료 소비 | 무료 보석 잔액 | 의상 구매 | 회수 보상 횟수 | 로봇 주간 과제 |','|---|---|---|---|---|---|---|---|---|---|---|---|']
    for x in s['rows']:lines.append(f"| {x['policy']} | {x['cohort']} | {x['clear42']}/{x['n']} | {fmt(x['cleared'])} | {fmt(x['day42'])} | {fmt(x['minutes'])} | {fmt(x['won'])} | {fmt(x['paidSpent'])} | {fmt(x['freeLeft'])} | {x['cosmeticOwners']}/{x['n']} | {fmt(x['salvageClaims'])} | {fmt(x['weeks'])} |")
    lines+=['','## 완료 전후 시간 분리','','최초 42작전 승리까지와 그 직후부터의 활동 시간이다. 완료 전에도 회수·주간 도전·실패·매 판 메뉴 30초를 포함한다. 보유 콘텐츠의 순수 길이나 사람의 실제 이용 시간이 아니다. 미완료 경로는 제외한다.','','| 조작 | 경로 | 최초 완료까지 분 | 완료 후 분 |','|---|---|---|---|']
    for x in s['rows']:lines.append(f"| {x['policy']} | {x['cohort']} | {fmt(x['completionMinutes'])} | {fmt(x['postCompletionMinutes'])} |")
    lines+=['','## 같은 시드의 보석 보존 경로와 비교','','완료 일수 절약 = 보석 보존 일수 − 비교 경로 일수. 음수는 비교 경로가 더 늦게 끝났다는 뜻이다. 양쪽 모두 완료한 짝만 일수에 포함한다.','','| 조작 | 경로 | 완료 일수 절약 | 빠름 / 같음 / 늦음 | 최종 작전 차이 |','|---|---|---|---|---|']
    for x in s['rows']:
        if x['cohort']=='reserve':continue
        pairs=[v for v in s['pairs'] if v['cohort']==x['cohort'] and v['policy']==x['policy']]
        days=[v['daysSaved'] for v in pairs if v['daysSaved'] is not None]
        counts=[sum(v>0 for v in days),sum(v==0 for v in days),sum(v<0 for v in days)]
        lines.append(f"| {x['policy']} | {x['cohort']} | {fmt(stat(days))} | {' / '.join(map(str,counts))} | {fmt(stat([v['stageDifference'] for v in pairs]))} |")
    lines+=['','## 마지막 7일과 장착 성장 상한','','무료 보석 증감은 최종일 잔액 − 7일 전 잔액이며 유료 보석 잔액을 제외한다. 그 전에 42작전을 완료한 표본에서는 최초 완료 보석 유입이 없는 구간이다. 장착 상한은 해당 대원 Lv.30과 장착 장비 3개 Lv.20이며 모든 대원·장비 수집 완료를 뜻하지 않는다.','','| 조작 | 경로 | 마지막 7일 이전 42 완료 | 무료 보석 증감 | 보급 구매 횟수 | 최종 장착 성장 상한 |','|---|---|---|---|---|---|']
    for x in s['rows']:lines.append(f"| {x['policy']} | {x['cohort']} | {x['completedBeforeLastWeek']}/{x['n']} | {fmt(x['lastWeekFreeChange'])} | {fmt(x['lastWeekKits'])} | {x['equippedGrowthCapped']}/{x['n']} |")
    lines+=['','## 가정','']+['- '+v for v in d['assumptions']]+['','실제 결제 없음. 도달하지 않은 경로의 완료 일수는 채우지 않음. 전체 시간에는 완료 후 반복도 포함하므로 캠페인 길이로 읽으면 안 됨. 각 행은 중앙값 [최소–최대].',f"\n원본 SHA-256: {s['sha256']}"]
    Path(a.markdown).write_text('\n'.join(lines)+'\n');print(f"Validated {s['journeys']} / {s['battles']}")
if __name__=='__main__':main()
