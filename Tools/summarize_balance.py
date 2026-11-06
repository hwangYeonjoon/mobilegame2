"""Summarize deterministic bot runs without presenting them as player success rates."""
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
    groups[(row['stage'], row['loadout'], row['policy'])].append(row)
result = []
for (stage, loadout, policy), rows in groups.items():
    values = dict(stage=stage, loadout=loadout, policy=policy, seeds=len(rows),
                  wins=sum(r['won'] for r in rows), budget_exhausted=sum(r['budget_exhausted'] for r in rows))
    for key in ('seconds', 'hp', 'minHp', 'damageReceived', 'healing', 'hazardDamage',
                'hazardHits', 'machines', 'machineUptime', 'machineDamageShare', 'bossSeconds', 'level'):
        values[key + '_mean'] = round(statistics.mean(r[key] for r in rows), 3)
    evolved = [r for r in rows if r['firstEvolution'] >= 0]
    values['runs_with_evolution'] = len(evolved)
    values['first_evolution_mean'] = round(statistics.mean(r['firstEvolution'] for r in evolved), 2) if evolved else None
    values['evolved_play_mean'] = round(statistics.mean(r['evolvedPlaySeconds'] for r in evolved), 2) if evolved else None
    result.append(values)
output = a.input.with_name('summary.csv')
with output.open('w', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=list(result[0])); writer.writeheader(); writer.writerows(result)
a.input.with_name('summary.json').write_text(json.dumps(dict(kind=data['kind'], rules=data['rules'], notes=data['notes'], groups=result), indent=2) + '\n')
for r in result:
    print(f"{r['stage']:2} {r['loadout']:19} {r['policy']:14}: {r['wins']}/{r['seeds']} wins, "
          f"machine {r['machineUptime_mean']:.1%}, hazard HP damage {r['hazardDamage_mean']:.0f}, boss {r['bossSeconds_mean']:.1f}s")
print('Bot data only:', output)
