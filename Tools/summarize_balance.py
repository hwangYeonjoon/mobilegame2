"""Report bot damage sources, reached-boss durations and paired growth, not player win rates."""
import argparse
import csv
import json
import statistics
from collections import defaultdict
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('input', type=Path)
a = p.parse_args()
data = json.loads(a.input.read_text())
groups = defaultdict(list)
for row in data['rows']:
    groups[(row['stage'], row['loadout'], row['policy'], row.get('machineEnabled', True))].append(row)
result = []
for (stage, loadout, policy, machine), rows in groups.items():
    values = dict(stage=stage, loadout=loadout, policy=policy, machine_enabled=machine, seeds=len(rows),
                  wins=sum(r['won'] for r in rows), early_deaths=sum(not r['won'] and r['seconds'] < 20 for r in rows),
                  budget_exhausted=sum(r['budget_exhausted'] for r in rows))
    for key in ('seconds', 'hp', 'minHp', 'damageReceived', 'healing', 'openingDamage',
                'machines', 'machineUptime', 'machineDamageShare', 'machineKills', 'machineIntercepts', 'level'):
        present = [r[key] for r in rows if key in r]
        values[key + '_mean'] = round(statistics.mean(present), 3) if present else None
    # Delayed warning damage includes enemies/bosses and excludes direct poison; never call it terrain.
    values['delayed_warning_damage_mean'] = round(statistics.mean(r['hazardDamage'] for r in rows), 3)
    total_damage = sum(r['damageReceived'] for r in rows)
    for i, name in enumerate(data.get('damageSources', [])):
        damage = sum(r['damageBySource'][i] for r in rows)
        values[name + '_damage_mean'] = round(damage / len(rows), 3)
        values[name + '_damage_share'] = round(damage / total_damage, 4) if total_damage else 0
    reached = [r for r in rows if r.get('bossReached', r['bossSeconds'] > 0)]
    boss_wins = [r for r in reached if r['won']]
    values['boss_reached'] = len(reached)
    values['boss_wins'] = len(boss_wins)
    values['boss_seconds_reached_mean'] = round(statistics.mean(r['bossSeconds'] for r in reached), 2) if reached else None
    values['boss_seconds_wins_mean'] = round(statistics.mean(r['bossSeconds'] for r in boss_wins), 2) if boss_wins else None
    values['boss_patterns_reached_mean'] = round(statistics.mean(r.get('bossPatterns', 0) for r in reached), 2) if reached else None
    evolved = [r for r in rows if r['firstEvolution'] >= 0]
    values['runs_with_evolution'] = len(evolved)
    values['first_evolution_mean'] = round(statistics.mean(r['firstEvolution'] for r in evolved), 2) if evolved else None
    values['first_evolution_duration_fraction'] = round(statistics.mean(r['firstEvolution']/r['duration'] for r in evolved), 3) if evolved and all('duration' in r for r in evolved) else None
    values['evolved_play_mean'] = round(statistics.mean(r['evolvedPlaySeconds'] for r in evolved), 2) if evolved else None
    active_seconds = sum(r['machineSeconds'] for r in rows)
    inactive_seconds = sum(r['seconds'] - r['machineSeconds'] for r in rows)
    if all('killsDuringMachine' in r for r in rows):
        values['active_kills_per_second'] = round(sum(r['killsDuringMachine'] for r in rows) / active_seconds, 2) if active_seconds else None
        values['inactive_kills_per_second'] = round(sum(r['kills'] - r['killsDuringMachine'] for r in rows) / inactive_seconds, 2) if inactive_seconds else None
    result.append(values)

lookup = {(r['stage'], r['policy'], r['seed'], r['loadout']): r for r in data['rows'] if r.get('machineEnabled', True)}
paired = []
for stage, policy in sorted({(r['stage'], r['policy']) for r in data['rows']}):
    for profile in ('assumed_progression', 'advanced_sensitivity'):
        pairs = [(r, lookup.get((stage, policy, r['seed'], profile))) for r in data['rows']
                 if r['stage'] == stage and r['policy'] == policy and r['loadout'] == 'fresh' and r.get('machineEnabled', True)]
        pairs = [(x, y) for x, y in pairs if y is not None]
        if pairs:
            paired.append(dict(stage=stage, policy=policy, upgraded=profile, seeds=len(pairs),
                loss_to_win=sum(not x['won'] and y['won'] for x, y in pairs),
                win_to_loss=sum(x['won'] and not y['won'] for x, y in pairs),
                both_win=sum(x['won'] and y['won'] for x, y in pairs),
                both_lose=sum(not x['won'] and not y['won'] for x, y in pairs)))
output = a.input.with_name('summary.csv')
with output.open('w', newline='') as f:
    fields = list(dict.fromkeys(key for row in result for key in row))
    writer = csv.DictWriter(f, fieldnames=fields); writer.writeheader(); writer.writerows(result)
a.input.with_name('summary.json').write_text(json.dumps(dict(kind=data['kind'], rules=data['rules'], notes=data['notes'], groups=result, paired_growth=paired), indent=2) + '\n')
for r in result:
    print(f"{r['stage']:2} {r['loadout']:21} {r['policy']:14} machine={r['machine_enabled']}: "
          f"{r['wins']}/{r['seeds']} wins, early deaths {r['early_deaths']}, evo {r['first_evolution_mean']}, "
          f"boss wins {r['boss_seconds_wins_mean']}s, terrain {r.get('Terrain_damage_share', 'unavailable')}")
print('Bot data only:', output)
