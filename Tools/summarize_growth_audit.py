#!/usr/bin/env python3
"""Validate and summarize reached equipment, never impute unreachable operations."""
import argparse
from collections import defaultdict
import json
import hashlib
import gzip
from pathlib import Path
from statistics import median

STRATEGIES = ('passive', 'balanced', 'attack_first')
BENEFITS = ('campaign_only', 'daily_claims', 'daily_plus_rewarded')
POLICIES = ('natural_legacy', 'controlled_ignore', 'controlled_avoid')
CURRENCIES = ('gold', 'parts', 'gems')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def stats(values):
    return {'median': median(values), 'min': min(values), 'max': max(values)} if values else None


def summarize(data):
    require(data['kind'] == 'economic_trajectory_simulation_not_human' and data['schema'] == 1, 'Wrong audit format')
    groups = defaultdict(list)
    seen = set()
    for journey in data['journeys']:
        key = tuple(journey[k] for k in ('strategy', 'benefits', 'policy'))
        unique = (*key, journey['seed'])
        require(unique not in seen, 'Duplicate journey')
        seen.add(unique)
        require(key[0] in STRATEGIES and key[1] in BENEFITS and key[2] in POLICIES, 'Unknown cohort')
        require(journey['maxStage'] == data['maxStage'] and journey['maxAttempts'] == data['maxAttempts'], 'Mixed retry or stage limits')
        wallet = dict(gold=1800, parts=30, gems=0)
        for seq, row in enumerate(journey['ledger'], 1):
            require(row['sequence'] == seq and row['before'] == wallet, 'Unlogged economic change')
            wallet = {c: wallet[c] + row['delta'][c] for c in CURRENCIES}
            require(wallet == row['after'] and min(wallet.values()) >= 0, 'Ledger does not reconcile')
        require(wallet == journey['final']['wallet'], 'Final wallet mismatch')
        stage, attempt, clears = 1, 1, 0
        for seq, run in enumerate(journey['attempts'], 1):
            require((run['stage'], run['attempt'], run['sequence']) == (stage, attempt, seq), 'Skipped attempt or stage')
            require(run['before']['unlockedStage'] == stage and stage not in run['before']['clearedStages'], 'Future reward leakage')
            require(run['before']['totalRuns'] == seq - 1 and run['after']['totalRuns'] == seq, 'Run count mismatch')
            require(attempt <= data['maxAttempts'], 'Retry budget exceeded')
            starts = [r for r in journey['ledger'] if r['stage'] == stage and r['attempt'] == attempt and r['action'] == 'battle_start']
            require(len(starts) == 1, 'Missing or duplicate battle start')
            settlements = [r for r in journey['ledger'] if r['stage'] == stage and r['attempt'] == attempt and r['action'] == 'battle_finish']
            require(len(settlements) == 1 and settlements[0]['delta'] == run['reward'], 'Receipt mismatch')
            if run['won']:
                clears = stage
                stage, attempt = stage + 1, 1
            else:
                attempt += 1
        require(clears == journey['clearedThrough'] and len(journey['attempts']) == journey['completedRuns'], 'Completion mismatch')
        if clears == data['maxStage']:
            require(journey['status'] == 'target_cleared' and journey['stoppedAt'] == 0, 'Invalid completed status')
        else:
            require(journey['status'] == 'retry_limit' and journey['stoppedAt'] == stage and attempt == data['maxAttempts'] + 1, 'Truncated journey')
        groups[key].append(journey)
    require(len(groups) == len(STRATEGIES) * len(BENEFITS) * len(POLICIES), 'Missing cohort')
    seed_sets = {tuple(sorted(j['seed'] for j in cohort)) for cohort in groups.values()}
    require(len(seed_sets) == 1 and len(next(iter(seed_sets))) == data['seedCount'], 'Unpaired or missing seeds')
    rows = []
    for (strategy, benefits, policy), cohort in sorted(groups.items()):
        for stage in range(1, data['maxStage'] + 1):
            runs = [r for j in cohort for r in j['attempts'] if r['stage'] == stage]
            first = [r for r in runs if r['attempt'] == 1]
            losses = [r for r in runs if not r['won']]
            entries = [r['before'] for r in first]
            entries_ledger = []
            for j in cohort:
                if any(r['stage'] == stage for r in j['attempts']):
                    end = next(i for i, r in enumerate(j['ledger']) if r['stage'] == stage and r['action'] == 'battle_start')
                    entries_ledger.append(j['ledger'][:end])
            rows.append(dict(strategy=strategy, benefits=benefits, policy=policy, stage=stage, journeys=len(cohort),
                reached=len(first), unreached=len(cohort)-len(first), firstWins=sum(r['won'] for r in first),
                cleared=sum(r['won'] for r in runs), attempts=len(runs),
                clearedPerReached=sum(r['won'] for r in runs)/len(first) if first else None,
                clearedPerJourney=sum(r['won'] for r in runs)/len(cohort),
                entryAttack=stats([s['attack'] for s in entries]), entryHealth=stats([s['health'] for s in entries]),
                entryHeroLevel=stats([s['heroLevel'] for s in entries]),
                entryGear=[dict(slot=slot, level=stats([s['equipped'][slot]['level'] for s in entries]),
                    rarity=stats([s['equipped'][slot]['rarity'] for s in entries]),
                    definitions=sorted(set(s['equipped'][slot]['definition'] for s in entries))) for slot in range(3)],
                entryWallet={c:stats([s['wallet'][c] for s in entries]) for c in CURRENCIES},
                priorGoldSpent=stats([-sum(min(0,r['delta']['gold']) for r in ledger) for ledger in entries_ledger]),
                priorOptionalGold=stats([sum(r['delta']['gold'] for r in ledger if r['action'] in ('attendance','missions','rewarded_result')) for ledger in entries_ledger]),
                entryShortGold=stats([r['preparation']['nextUpgrade']['missingGold'] for r in first if r['preparation']['nextUpgrade']]),
                entryShortParts=stats([r['preparation']['nextUpgrade']['missingParts'] for r in first if r['preparation']['nextUpgrade']]),
                deferredAffordable=sum(r['preparation']['nextUpgrade'] is not None and r['preparation']['nextUpgrade']['canAfford'] for r in first),
                failuresBeforeBoss=sum(not r['bossReached'] for r in losses), failuresAtBoss=sum(r['bossReached'] for r in losses),
                lossCauses={cause:sum(r['cause']==cause for r in losses) for cause in sorted(set(r['cause'] for r in losses))},
                bossSeconds=stats([r['bossSeconds'] for r in runs if r['bossReached']]),
                damageReceived=sum(r['damageReceived'] for r in runs), bossContactDamage=sum(r['bossContactDamage'] for r in runs),
                bossWarningDamage=sum(r['bossWarningDamage'] for r in runs)))
    return dict(kind='economic_trajectory_summary_not_human', schema=1, build=data['build'], rules=data['rules'],
        seedCount=data['seedCount'], maxStage=data['maxStage'], maxAttempts=data['maxAttempts'],
        journeyCount=len(data['journeys']), runCount=sum(j['completedRuns'] for j in data['journeys']), assumptions=data['assumptions'], rows=rows)


def markdown(report):
    lines = ['# Economic trajectory audit', '',
        f"Build {report['build']}, rules {report['rules']}; {report['journeyCount']} journeys / {report['runCount']} battles.",
        f"{report['seedCount']} paired seeds per cohort; at most {report['maxAttempts']} attempts per operation.",
        f"Source SHA-256: {report.get('sourceSha256', 'not recorded')}", '',
        'These are behavioral scenarios, not average human equipment or participant outcomes. Equipment medians include only first entries that reached the stage (survivor bias).',
        'Clear counts include retries; denominators are reached journeys, not battles. Unreached equipment is —, never zero. Each cohort must be read separately.', '']
    previous = None
    for row in report['rows']:
        key = (row['strategy'],row['benefits'],row['policy'])
        if key != previous:
            lines += ['', '## ' + ' / '.join(key), '',
                '| Stage | Reached / all | First wins / reached | Clears / reached | Attempts | Entry ATK median [range] | Entry HP median [range] | W/A/M/H levels median | Loss preboss / boss |',
                '|---|---|---|---|---|---|---|---|---|']
            previous = key
        def fmt(s):
            return '—' if s is None else f"{s['median']:g} [{s['min']:g}–{s['max']:g}]"
        levels = [g['level'] for g in row['entryGear']] + [row['entryHeroLevel']]
        level_text = '/'.join('—' if s is None else f"{s['median']:g}" for s in levels)
        first_ratio = f"{row['firstWins']}/{row['reached']}" if row['reached'] else '—'
        clear_ratio = f"{row['cleared']}/{row['reached']}" if row['reached'] else '—'
        lines.append(f"| {row['stage']} | {row['reached']}/{row['journeys']} | {first_ratio} | {clear_ratio} | {row['attempts']} | {fmt(row['entryAttack'])} | {fmt(row['entryHealth'])} | {level_text} | {row['failuresBeforeBoss']}/{row['failuresAtBoss']} |")
    return '\n'.join(lines) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path)
    parser.add_argument('--json', type=Path, required=True)
    parser.add_argument('--markdown', type=Path, required=True)
    args = parser.parse_args()
    source = args.input.read_bytes()
    if args.input.suffix == '.gz':
        source = gzip.decompress(source)
    report = summarize(json.loads(source))
    report['sourceSha256'] = hashlib.sha256(source).hexdigest()
    args.json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    args.markdown.write_text(markdown(report))
    print(f"Validated {report['journeyCount']} journeys, {report['runCount']} battles; wrote {args.json} and {args.markdown}")


if __name__ == '__main__':
    main()
