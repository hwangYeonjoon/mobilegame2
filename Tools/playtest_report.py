"""Aggregate explicitly exported playtest files; no network access or account identifiers."""
import argparse
import collections
import datetime as dt
import json
from pathlib import Path


def summarize(paths, as_of=None):
    people = collections.defaultdict(dict)
    excluded = 0
    for path in paths:
        data = json.loads(Path(path).read_text())
        participant = data.get('participant')
        if not participant:
            continue
        for event in data.get('events', []):
            if event.get('test'):
                excluded += 1
                continue
            if event.get('id'):
                people[participant][event['id']] = event
    counts = collections.Counter()
    groups = collections.defaultdict(set)
    gems_in = gems_out = 0
    starts, ends = collections.Counter(), collections.Counter()
    days = []
    for participant, indexed in people.items():
        observed = set()
        for event in indexed.values():
            when = dt.datetime.fromisoformat(event['at'].replace('Z', '+00:00')).date()
            observed.add(when)
            key = event['kind'] + ':' + event['subject']
            counts[key] += 1
            groups[key].add(participant)
            if event['kind'] == 'economy' and event['subject'] == 'gems':
                amount = event['number']
                gems_in += max(0, amount)
                gems_out += max(0, -amount)
            if event['kind'] == 'battle_start':
                starts[str(event['number'] + 1) + ':' + event['subject']] += 1
            if event['kind'] == 'battle_end':
                ends[str(event['number'] + 1) + ':' + event['subject']] += 1
        if observed:
            days.append(observed)
    cutoff = as_of or dt.datetime.now(dt.timezone.utc).date()
    retention = {}
    for offset in (1, 7):
        mature = [d for d in days if min(d) + dt.timedelta(days=offset) < cutoff]
        returned = sum(min(d) + dt.timedelta(days=offset) in d for d in mature)
        retention['day_' + str(offset)] = {'eligible': len(mature), 'returned': returned,
            'rate': returned / len(mature) if mature else None}
    return {'participants': len(days), 'excluded_test_events': excluded,
            'events': dict(counts), 'interested_participants': {k: len(v) for k, v in groups.items() if 'interest:' in k},
            'gems_earned': gems_in, 'gems_spent': gems_out,
            'battle_starts': dict(starts), 'battle_ends': dict(ends),
            'observed_retention': retention,
            'limits': 'Opt-in sample; first observed day is not installation day. 14-day local retention, voluntary exports and consent changes can bias results. Interest is not a cash purchase; cosmetic_purchase uses game gems. Unfinished battles are not proven quits.'}

if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('files', nargs='+', type=Path)
    p.add_argument('--output', type=Path)
    args = p.parse_args()
    report = json.dumps(summarize(args.files), ensure_ascii=False, indent=2)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report + '\n')
    else:
        print(report)
