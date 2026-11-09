#!/usr/bin/env python3
"""Summarize paired boss audits without pooling different bot policies/loadouts."""
import argparse
import json
from collections import defaultdict
from statistics import median


def indexed(path):
    data = json.loads(path.read_text())
    if not data.get('bossRamp') or data.get('kind') != 'automated_simulation_not_human':
        raise ValueError(f'{path}: expected --boss-ramp-audit data')
    rows = {}
    for row in data['rows']:
        key = tuple(row[k] for k in ('stage', 'loadout', 'policy', 'seed', 'robot'))
        if key in rows:
            raise ValueError(f'{path}: duplicate pair {key}')
        if row['budget_exhausted']:
            raise ValueError(f'{path}: unfinished run {key}')
        rows[key] = row
    if not rows:
        raise ValueError(f'{path}: no runs')
    return data, rows


def summary(before_path, after_path, growth_change=False):
    before_meta, before = indexed(before_path)
    after_meta, after = indexed(after_path)
    if before.keys() != after.keys():
        raise ValueError('Before/after must contain exactly the same stage/loadout/policy/seed/robot pairs')
    groups = defaultdict(list)
    for key, row in after.items():
        old = before[key]
        if (old['heroLevel'],old['itemLevels']) != (row['heroLevel'],row['itemLevels']):
            raise ValueError(f'Equipment levels changed in pair {key}')
        if not growth_change and (old['attack'], old['health']) != (row['attack'], row['health']):
            raise ValueError(f'Loadout changed in pair {key}')
        groups[key[:3]].append((old, row))
    print(f'Automated simulation only; rules {before_meta["rules"]} -> {after_meta["rules"]}; {len(after)} paired runs per version.')
    if growth_change:
        print('Growth formula comparison: same equipment/hero levels, intentionally changed ATK/HP shown before -> after.')
    print('Three robot targets reuse each seed; these are not independent human participants.')
    print('Boss seconds include losses. Damage shares use actual HP loss, exclude absorbed/immune damage.')
    print('| Stage | Loadout (ATK/HP) | Policy | Before wins | After wins | Loss->win / win->loss | Boss seconds median | End HP median | Boss contact / warning share | Peak boss warnings |')
    print('|---|---|---|---|---|---|---|---|---|---|')
    for (stage, loadout, policy), pairs in sorted(groups.items()):
        rows = [new for _, new in pairs]
        old_wins = sum(old['won'] for old, _ in pairs)
        wins = sum(row['won'] for row in rows)
        gained = sum(not old['won'] and new['won'] for old, new in pairs)
        lost = sum(old['won'] and not new['won'] for old, new in pairs)
        total = sum(row['bossContactDamage'] + row['bossWarningDamage'] for row in rows)
        contact = sum(row['bossContactDamage'] for row in rows) / total if total else 0
        warning = sum(row['bossWarningDamage'] for row in rows) / total if total else 0
        stats = rows[0]
        peaks = [row['peakBossWarnings'] for row in rows if 'peakBossWarnings' in row]
        peak = max(peaks) if peaks else 'unmeasured'
        power = f'{pairs[0][0]["attack"]}/{pairs[0][0]["health"]} -> {stats["attack"]}/{stats["health"]}' if growth_change else f'{stats["attack"]}/{stats["health"]}'
        print(f'| {stage} | {loadout} ({power}) | {policy} | {old_wins}/{len(rows)} | {wins}/{len(rows)} | {gained}/{lost} | {median(r["bossSeconds"] for r in rows):.1f} | {median(max(0,r["hp"]) for r in rows):.0f} | {contact:.1%} / {warning:.1%} | {peak} |')


if __name__ == '__main__':
    from pathlib import Path
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('before', type=Path)
    parser.add_argument('after', type=Path)
    parser.add_argument('--growth-change',action='store_true',help='Compare a changed growth formula while requiring identical equipment/hero levels')
    args = parser.parse_args()
    summary(args.before, args.after, args.growth_change)
