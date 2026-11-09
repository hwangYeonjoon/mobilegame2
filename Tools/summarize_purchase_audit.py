#!/usr/bin/env python3
"""Audit local paid/free provenance and calendar-matched progression. Not human evidence."""
import argparse
from collections import Counter, defaultdict
from datetime import datetime, timedelta
import gzip
import hashlib
import json
from pathlib import Path
from statistics import median

COHORTS=('none','ads_only','kit_free','pass_free','both_free','kit_paid','pass_paid','both_paid')
SUPPORTED=COHORTS+('both_free_kit_first',)
POLICIES=('natural_legacy','controlled_avoid')
CURRENCIES=('gold','parts','gems')
MILESTONES=(6,12,18,24,30,36,42)

def require(value,message):
    if not value: raise ValueError(message)

def stats(xs):
    return dict(median=median(xs),min=min(xs),max=max(xs)) if xs else None

def load(path):
    raw=Path(path).read_bytes()
    if str(path).endswith('.gz'): raw=gzip.decompress(raw)
    return json.loads(raw),hashlib.sha256(raw).hexdigest()

def validate(data):
    require(data['kind']=='purchase_calendar_simulation_not_human' and data['schema']==1,'Wrong purchase audit schema')
    selected=data['cohorts']
    require(bool(selected) and len(set(selected))==len(selected) and set(selected)<=set(SUPPORTED) and tuple(data['policies'])==POLICIES,'Unknown cohorts/policies')
    packs={p['id']:p for p in data['packs']}
    start=(datetime.fromisoformat(data['start'])+timedelta(hours=4)).date()
    all_keys=set();groups=defaultdict(list)
    for j in data['journeys']:
        key=(j['cohort'],j['policy'],j['seed']);require(key not in all_keys,'Duplicate journey');all_keys.add(key)
        require(key[0] in selected and key[1] in POLICIES,'Unknown journey');groups[key[:2]].append(j)
        wallet=dict(gold=1800,parts=30,gems=0);paid=0;cleared=set();pass_owned=False;claimed=set();date_counts=Counter();daily_runs=Counter();daily_kills=Counter()
        free_in=free_out=paid_in=paid_out=won_price=ads=0;started=None;last_finished=None;result_claimed=set();round_index=0;attendances=0;previous_day=0
        for seq,row in enumerate(j['ledger'],1):
            require(row['sequence']==seq and row['before']==wallet and row['paidBefore']==paid,'Unlogged wallet change')
            after={c:wallet[c]+row['delta'][c] for c in CURRENCIES}
            require(after==row['after'] and min(after.values())>=0,'Ledger delta mismatch')
            require(0<=row['paidAfter']<=after['gems'],'Invalid paid balance')
            service=(datetime.fromisoformat(row['serviceDate']).date()-start).days+1
            require(1<=service<=data['days'],'Outside calendar budget')
            require(service>=previous_day,'Service clock moved backwards');previous_day=service
            action=row['action'];date_counts[(service,action)]+=1
            dp=row['paidAfter']-paid;df=row['delta']['gems']-dp
            if action=='simulated_topup':
                require(j['cohort'].endswith('_paid') and row['target'] in packs,'Unfunded top-up')
                p=packs[row['target']];require(row['delta']==dict(gold=0,parts=0,gems=p['gems']) and dp==p['gems'],'Wrong pack grant')
                needs_pass=j['cohort'].startswith(('pass_','both_')) and not pass_owned
                needs_kit=j['cohort'].startswith(('kit_','both_')) and date_counts[(service,'buy_kit')]==0
                missing=(900 if needs_pass else 0)+(100 if needs_kit else 0)-wallet['gems']
                suggestion=next((x for x in data['packs'] if x['gems']>=missing),None)
                require(missing>0 and suggestion and suggestion['id']==row['target'],'Unnecessary or oversized top-up')
                won_price+=p['proposedWon']
            else: require(dp<=0,'Paid currency minted outside top-up')
            if row['delta']['gems']<0:
                spend=-row['delta']['gems'];require(row['paidAfter']==paid-max(0,spend-(wallet['gems']-paid)),'Free-first spend violated')
            if action=='buy_kit':
                require(j['cohort'].startswith(('kit_','both_')) and date_counts[(service,action)]<=1,'Daily kit cap or cohort violated')
                require(row['delta']==dict(gold=4500,parts=40,gems=-100),'Wrong kit amount')
            elif action=='buy_pass':
                require(j['cohort'].startswith(('pass_','both_')) and not pass_owned,'Duplicate/unexpected pass purchase')
                require(row['delta']==dict(gold=0,parts=0,gems=-900),'Wrong pass price');pass_owned=True
            elif action in ('pass_free_claim','pass_premium_claim'):
                tier=int(row['target']);premium=action=='pass_premium_claim'
                require(tier in range(4) and (tier+1)*3 in cleared,'Unearned pass reward')
                require(not premium or pass_owned,'Unowned premium reward')
                require((premium,tier) not in claimed,'Duplicate pass reward');claimed.add((premium,tier))
                require(row['delta']==dict(gold=3000+1000*tier if premium else 1000+500*tier,parts=30+10*tier if premium else 10+5*tier,gems=0),'Wrong pass reward')
            elif action=='attendance':
                require(date_counts[(service,action)]==1,'Duplicate attendance')
                expected=[dict(gold=1000,parts=0,gems=0),dict(gold=0,parts=20,gems=0),dict(gold=0,parts=0,gems=50),
                    dict(gold=2000,parts=0,gems=0),dict(gold=0,parts=40,gems=0),dict(gold=0,parts=0,gems=100),
                    dict(gold=0,parts=0 if attendances==6 else 60,gems=0)][attendances%7]
                require(row['delta']==expected,'Wrong attendance reward');attendances+=1
            elif action=='missions':
                require(date_counts[(service,action)]==1 and daily_runs[service]>=3 and daily_kills[service]>=500,'Unearned mission reward')
                require(row['delta']==dict(gold=2000,parts=20,gems=30),'Wrong mission reward')
            elif action.startswith('rewarded_'):
                require(j['cohort']=='ads_only','Ads leaked into purchase comparison');ads+=1
                if action=='rewarded_parts':
                    require(date_counts[(service,action)]<=2,'Repair ad daily cap')
                    require(row['delta']==dict(gold=min(2400,600+100*len(cleared)),parts=min(24,8+2*(len(cleared)//3)),gems=0),'Wrong repair ad reward')
                else:
                    require(action=='rewarded_result' and date_counts[(service,action)]<=3,'Result ad daily cap')
                    require(last_finished is not None and last_finished[1]==service and last_finished[0] not in result_claimed,'Duplicate/stale result ad')
                    result_claimed.add(last_finished[0]);require(row['delta']==dict(gold=last_finished[2]//2,parts=0,gems=0),'Wrong result ad reward')
            elif action=='battle_start':
                require(started is None and round_index<len(j['runs']),'Duplicate battle start');r=j['runs'][round_index];b=r['battle']
                require(row['target'] not in result_claimed and r['day']==service and b['before']['wallet']==wallet,'Wrong battle entry')
                require(b['sequence']==round_index+1 and b['before']['totalRuns']==round_index,'Wrong round index')
                require(set(b['before']['clearedStages'])==cleared,'Future progression leaked')
                require((b['stage'] in cleared) if r['farm'] else b['stage']==len(cleared)+1,'Future operation or invalid farm')
                require(not any(row['delta'].values()),'Battle start minted rewards');started=row['target']
            elif action=='battle_finish':
                require(started==row['target'] and started is not None,'Unmatched settlement');r=j['runs'][round_index];b=r['battle']
                require(row['delta']==b['reward'] and row['after']==b['after']['wallet'],'Receipt mismatch')
                require(b['after']['totalRuns']==round_index+1,'Post-run counter mismatch')
                require(b['reward']['gems']==(50 if b['won'] and not r['farm'] else 0),'Duplicate first-clear gems')
                if b['won'] and not r['farm']: cleared.add(b['stage'])
                require(set(b['after']['clearedStages'])==cleared,'Invalid clear state')
                if b['seconds']>=30: daily_runs[service]+=1
                daily_kills[service]+=b['kills'];last_finished=(started,service,row['delta']['gold']);started=None;round_index+=1
            elif action=='upgrade':
                slot=int(row['target'].split(':')[0]);level=int(row['target'].rsplit(':',1)[1])
                require(row['delta']==dict(gold=-level*(500 if slot==3 else 300),parts=-level*(3 if slot==3 else 2),gems=0),'Wrong upgrade cost')
            elif action in ('transfer','equip'):require(not any(row['delta'].values()),'Equipment command changed wallet')
            else: require(action=='simulated_topup','Unknown economic action')
            free_in+=max(0,df);free_out+=max(0,-df);paid_in+=max(0,dp);paid_out+=max(0,-dp);wallet=after;paid=row['paidAfter']
        require(started is None and round_index==len(j['runs']),'Unsettled run')
        require(wallet==j['final']['wallet'] and paid==j['paidGems'],'Final balance mismatch')
        require((free_in,free_out,paid_in,paid_out,won_price,ads)==tuple(j[k] for k in ('freeEarned','freeSpent','paidGranted','paidSpent','proposedWon','adsViewed')),'Funding totals mismatch')
        require(j['clearedThrough']==len(cleared),'Progress total mismatch')
        tries=Counter();losses=0;frontier=1
        for r in j['runs']:
            b=r['battle'];require(r['farm']==(losses>=2 and frontier>1),'Recovery policy drift')
            if r['farm']: require(b['stage']==frontier-1,'Farm target drift')
            tries[(r['farm'],b['stage'])]+=1;n=tries[(r['farm'],b['stage'])]
            require(b['attempt']==n and b['seed']==j['seed']+(b['stage']-1)*7919+(n-1)*104729+(1000003 if r['farm'] else 0),'Unpaired combat seed')
            if r['farm']:losses=0
            elif b['won']:frontier+=1;losses=0
            else:losses+=1
        require(len(j['days'])==data['days'],'Missing calendar checkpoint')
        for day,d in enumerate(j['days'],1):
            rounds=[r for r in j['runs'] if r['day']==day]
            require(d['day']==day and [r['slot'] for r in rounds]==list(range(1,len(rounds)+1)),'Wrong day slots')
            require(len(rounds)<=data['slots'],'Sortie budget exceeded')
            if not d['finished']:require(len(rounds)==data['slots'],'Unfinished day silently truncated')
            past=[r for r in j['runs'] if r['day']<=day];progress=sum(not r['farm'] and r['battle']['won'] for r in past)
            require(d['runs']==len(past) and d['clearedThrough']==progress and d['state']['totalRuns']==len(past),'Wrong day progress')
            prefix=j['ledger'][:d['ledgerEnd']]
            require(bool(prefix) and prefix[-1]['after']==d['state']['wallet'] and prefix[-1]['paidAfter']==d['paidGems'],'Day wallet mismatch')
            play=sum(r['battle']['seconds'] for r in past);active=play+30*len(past)+30*sum(x['action'].startswith('rewarded_') for x in prefix)
            require(abs(d['playSeconds']-play)<.25 and abs(d['activeSeconds']-active)<.25,'Play time mismatch')
        done=next((r['day'] for r in j['runs'] if not r['farm'] and r['battle']['stage']==data['maxStage'] and r['battle']['won']),0)
        require(j['completedDay']==done and j['status']==('target_cleared' if done else 'calendar_budget_exhausted'),'Incorrect completion/censoring')
    require(len(groups)==len(selected)*len(POLICIES),'Missing cohort')
    seeds={tuple(sorted(j['seed'] for j in group)) for group in groups.values()}
    require(len(seeds)==1 and len(next(iter(seeds)))==data['seedCount'],'Missing/unpaired seed sets')
    return groups

def merge_audits(paths):
    import copy
    result=None;sources=[]
    fields=('kind','schema','build','rules','seedCount','days','maxStage','slots','start','strategy','robotTarget','packs','policies')
    for path in paths:
        data,sha=load(path);validate(data)
        sources.append(dict(file=Path(path).name,sha256=sha))
        if result is None:result=copy.deepcopy(data);continue
        require(all(result[k]==data[k] for k in fields),'Mixed audit conditions')
        require(not set(result['cohorts'])&set(data['cohorts']),'Overlapping merge cohorts')
        result['cohorts']+=data['cohorts'];result['journeys']+=data['journeys']
        result['assumptions']=list(dict.fromkeys(result['assumptions']+data['assumptions']))
    require(result is not None,'No audit inputs')
    result['sources']=sources;validate(result)
    return result

def milestone(j,stage):
    return next((r for r in j['runs'] if not r['farm'] and r['battle']['stage']==stage and r['battle']['won']),None)

def summarize(data):
    groups=validate(data);rows=[];stages=[];paired=[];source=[];calendar=[]
    for (cohort,policy),js in sorted(groups.items()):
        rows.append(dict(cohort=cohort,policy=policy,n=len(js),cleared=stats([j['clearedThrough'] for j in js]),
            clear18=sum(j['clearedThrough']>=18 for j in js),clear30=sum(j['clearedThrough']>=30 for j in js),clear42=sum(j['clearedThrough']>=42 for j in js),
            firstCompletionDay=stats([j['completedDay'] for j in js if j['completedDay']]),activeMinutes=stats([j['activeSeconds']/60 for j in js]),
            paidGemsGranted=stats([j['paidGranted'] for j in js]),paidGemsSpent=stats([j['paidSpent'] for j in js]),freeGemsEarned=stats([j['freeEarned'] for j in js]),
            firstClearFreeGems=stats([sum(x['delta']['gems'] for x in j['ledger'] if x['action']=='battle_finish') for j in js]),
            recurringFreeGems=stats([sum(x['delta']['gems'] for x in j['ledger'] if x['action'] in ('attendance','missions')) for j in js]),
            freeGemsSpent=stats([j['freeSpent'] for j in js]),paidGemsLeft=stats([j['paidGems'] for j in js]),proposedWon=stats([j['proposedWon'] for j in js]),
            kits=stats([sum(x['action']=='buy_kit' for x in j['ledger']) for j in js]),passOwners=sum(any(x['action']=='buy_pass' for x in j['ledger']) for j in js),
            ads=stats([j['adsViewed'] for j in js]),farms=stats([sum(r['farm'] for r in j['runs']) for j in js])))
        for stage in [s for s in MILESTONES if s<=data['maxStage']]:
            first=[r['battle'] for j in js for r in j['runs'] if not r['farm'] and r['battle']['stage']==stage and r['battle']['attempt']==1]
            entries=[b['before'] for b in first]
            wins=[milestone(j,stage) for j in js];wins=[r for r in wins if r]
            stages.append(dict(cohort=cohort,policy=policy,stage=stage,n=len(js),reached=len(entries),cleared=len(wins),
                entryAttack=stats([r['attack'] for r in entries]),entryHealth=stats([r['health'] for r in entries]),
                firstAttemptWins=sum(b['won'] for b in first),firstAttemptMachines=stats([b['machines'] for b in first]),
                firstAttemptKillsPerMinute=stats([b['kills']*60/b['seconds'] for b in first if b['seconds']>0]),firstAttemptEndLevel=stats([b['level'] for b in first]),
                clearDay=stats([r['day'] for r in wins]),totalSortiesToClear=stats([r['battle']['sequence'] for r in wins])))
        for action in sorted(set(x['action'] for j in js for x in j['ledger'])):
            source.append(dict(cohort=cohort,policy=policy,action=action,gold=stats([sum(x['delta']['gold'] for x in j['ledger'] if x['action']==action) for j in js]),parts=stats([sum(x['delta']['parts'] for x in j['ledger'] if x['action']==action) for j in js])))
        if cohort=='none':continue
        baselines=['none']+([cohort.replace('_paid','_free')] if cohort.endswith('_paid') else [])
        if cohort=='both_paid':baselines.append('both_free_kit_first')
        for baseline in baselines:
            if (baseline,policy) not in groups:continue
            control={j['seed']:j for j in groups[(baseline,policy)]}
            for j in js:
                base=control[j['seed']]
                for stage in [s for s in MILESTONES if s<=data['maxStage']]:
                    a,b=milestone(base,stage),milestone(j,stage)
                    paired.append(dict(baseline=baseline,cohort=cohort,policy=policy,seed=j['seed'],stage=stage,
                        outcome='both_cleared' if a and b else 'variant_cleared_only' if b else 'control_cleared_only' if a else 'neither_cleared',
                        sortiesSaved=a['battle']['sequence']-b['battle']['sequence'] if a and b else None,daysSaved=a['day']-b['day'] if a and b else None))
                for a,b in zip(base['days'],j['days']):
                    calendar.append(dict(baseline=baseline,cohort=cohort,policy=policy,seed=j['seed'],day=a['day'],
                        clearedDifference=b['clearedThrough']-a['clearedThrough'],attackDifference=b['state']['attack']-a['state']['attack'],
                        healthDifference=b['state']['health']-a['state']['health'],activeMinutesDifference=(b['activeSeconds']-a['activeSeconds'])/60))
    return dict(kind='purchase_calendar_summary_not_human',schema=1,build=data['build'],rules=data['rules'],days=data['days'],slots=data['slots'],maxStage=data['maxStage'],
        journeys=len(data['journeys']),battles=sum(len(j['runs']) for j in data['journeys']),seedCount=data['seedCount'],assumptions=data['assumptions'],rows=rows,stages=stages,paired=paired,calendarPairs=calendar,resourceSources=source)

def fmt(s):return '—' if s is None else f"{s['median']:g} [{s['min']:g}–{s['max']:g}]"

def markdown(r):
    lines=['# Purchase / free progression audit','',f"Build {r['build']}, rules {r['rules']}; {r['journeys']} trajectories / {r['battles']} real simulated battles.",
        f"{r['days']} calendar days × up to {r['slots']} sorties/day, target {r['maxStage']}; {r['seedCount']} paired seeds per cohort.",
        f"Source SHA-256: {r.get('sourceSha256','unrecorded')}",'',
        'Offline scenario evidence only: no actual payments, ad views or participants. ProposedWon uses source-code price proposals; it is NOT current Play pricing. All groups receive the free pass. Minutes differ despite matched calendar/slots.','',
        'none = no gems spent; ads_only = no purchases + assumed successful ads; *_free = earned gems only; *_paid = earned gems first + smallest sufficient default catalog pack for shortfalls. Both reserves for the pass first, except both_free_kit_first (daily kit first; remaining gems saved for pass).','']
    for policy in POLICIES:
        lines += ['## '+policy,'','| Cohort | Cleared stage median [range] | 18 / 30 / 42 clears | Active min | Kit count | Pass owners | Free gems spent | Paid granted / spent / left | Proposed won |','|---|---|---|---|---|---|---|---|---|']
        for x in r['rows']:
            if x['policy']!=policy:continue
            lines.append(f"| {x['cohort']} | {fmt(x['cleared'])} | {x['clear18']}/{x['n']} · {x['clear30']}/{x['n']} · {x['clear42']}/{x['n']} | {fmt(x['activeMinutes'])} | {fmt(x['kits'])} | {x['passOwners']}/{x['n']} | {fmt(x['freeGemsSpent'])} | {fmt(x['paidGemsGranted'])} / {fmt(x['paidGemsSpent'])} / {fmt(x['paidGemsLeft'])} | {fmt(x['proposedWon'])} |")
    lines += ['','## Reached-only equipment and progression','', 'Unreached equipment is —. These medians have survivor bias and are not average player equipment.','',
        '| Policy | Cohort | Stage | Reached/all | Cleared/all | Entry ATK | Entry HP | Clear day | Total sorties to clear |','|---|---|---|---|---|---|---|---|---|']
    for x in r['stages']:lines.append(f"| {x['policy']} | {x['cohort']} | {x['stage']} | {x['reached']}/{x['n']} | {x['cleared']}/{x['n']} | {fmt(x['entryAttack'])} | {fmt(x['entryHealth'])} | {fmt(x['clearDay'])} | {fmt(x['totalSortiesToClear'])} |")
    lines+=['','## Free gem sources','','First clears are finite. This new-account study cannot establish long-term daily-kit affordability after campaign completion.','',
        '| Policy | Cohort | First-clear gems | Attendance/mission gems | All free gems earned | Free gems spent |','|---|---|---|---|---|---|']
    for x in r['rows']:lines.append(f"| {x['policy']} | {x['cohort']} | {fmt(x['firstClearFreeGems'])} | {fmt(x['recurringFreeGems'])} | {fmt(x['freeGemsEarned'])} | {fmt(x['freeGemsSpent'])} |")
    lines+=['','## Combat opportunities at first entry','','Robot activations, kill rate and build level are mechanical observations, NOT enjoyment or willingness to pay. Compare the same stage and policy. A shorter victorious run can have fewer robot activations.','',
        '| Policy | Cohort | Stage | First wins/reached | Robot activations | Kills/min | End skill level |','|---|---|---|---|---|---|---|']
    for x in r['stages']:lines.append(f"| {x['policy']} | {x['cohort']} | {x['stage']} | {str(x['firstAttemptWins'])+'/'+str(x['reached']) if x['reached'] else '—'} | {fmt(x['firstAttemptMachines'])} | {fmt(x['firstAttemptKillsPerMinute'])} | {fmt(x['firstAttemptEndLevel'])} |")
    lines+=['','## Paired savings against no-purchase and earned-gem controls','','Savings are computed only where BOTH paths clear the same milestone. Missing clears are never assigned zero or an assumed completion time. Paid vs matching free purchase isolates funding policy better than comparing only with no gem spending.','',
        '| Policy | Baseline → cohort | Stage | Both clear | Variant-only / control-only / neither | Sorties saved median [range] | Days saved |','|---|---|---|---|---|---|---|']
    grouped=defaultdict(list)
    for x in r['paired']:grouped[(x['policy'],x['baseline'],x['cohort'],x['stage'])].append(x)
    for (p,b,c,s),xs in sorted(grouped.items()):
        counts=Counter(x['outcome'] for x in xs);lines.append(f"| {p} | {b} → {c} | {s} | {counts['both_cleared']}/{len(xs)} | {counts['variant_cleared_only']} / {counts['control_cleared_only']} / {counts['neither_cleared']} | {fmt(stats([x['sortiesSaved'] for x in xs if x['sortiesSaved'] is not None]))} | {fmt(stats([x['daysSaved'] for x in xs if x['daysSaved'] is not None]))} |")
    lines+=['','## Assumptions','']+['- '+s for s in r['assumptions']]
    return '\n'.join(lines)+'\n'

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('input',type=Path,nargs='?');p.add_argument('--merge',type=Path,nargs='+');p.add_argument('--output',type=Path);p.add_argument('--json',type=Path,required=True);p.add_argument('--markdown',type=Path,required=True);a=p.parse_args()
    if a.merge:
        if a.input or not a.output:p.error('--merge requires --output and no positional input')
        data=merge_audits(a.merge);raw=(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n').encode();a.output.write_bytes(raw);sha=hashlib.sha256(raw).hexdigest()
    else:
        if not a.input or a.output:p.error('Provide input, or --merge with --output')
        data,sha=load(a.input)
    r=summarize(data);r['sourceSha256']=sha;a.json.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');a.markdown.write_text(markdown(r));print(f"Validated {r['journeys']} trajectories / {r['battles']} battles")
if __name__=='__main__':main()
