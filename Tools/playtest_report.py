"""Aggregate explicitly exported playtest files; no network access or account identifiers."""
import argparse
import collections
import datetime as dt
import json
import statistics
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
    robot_samples = collections.defaultdict(lambda: collections.defaultdict(list))
    discovery = collections.defaultdict(collections.Counter)
    counts = collections.Counter()
    groups = collections.defaultdict(set)
    gems_in = gems_out = 0
    starts, ends = collections.Counter(), collections.Counter()
    days = []
    cohorts = collections.defaultdict(lambda: {"participants": set(), "finished_runs": 0, "machines": [], "frame_p95_ms": [], "replay_interest_responses": collections.Counter()})
    for participant, indexed in people.items():
        observed = set()
        for event in indexed.values():
            when = dt.datetime.fromisoformat(event['at'].replace('Z', '+00:00')).date()
            observed.add(when)
            cohort = cohorts[(event.get('version') or 'unknown') + ' / ' + (event.get('platform') or 'unknown')]
            cohort['participants'].add(participant)
            if event['kind'] == 'combat_outcome' and event['subject'] == 'machines':
                cohort['finished_runs'] += 1
                cohort['machines'].append(event.get('number', 0))
            if event['kind'] == 'performance' and event['subject'] == 'frame_p95_ms' and event.get('number', 0) > 0:
                cohort['frame_p95_ms'].append(event.get('value', 0))
            if event['kind'] == 'feedback' and event['subject'] == 'replay_interest':
                cohort['replay_interest_responses'][str(event.get('number'))] += 1
            robot_key = (event.get('version') or 'unknown') + ' / ' + (event.get('platform') or 'unknown') + ' / stage_' + str(event.get('number', 0))
            if event['kind'] == 'robot_milestone' and event['subject'] in ('parts_ready', 'first_craft', 'first_board', 'first_specialized_board'):
                value = event.get('value')
                if isinstance(value, (int, float)) and 0 <= value <= 600:
                    robot_samples[robot_key][event['subject']].append(value)
            if event['kind'] == 'robot_discovery':
                discovery[robot_key][event['subject']] += 1
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
    quality = {}
    for key, cohort in cohorts.items():
        machines = cohort['machines']; frames = cohort['frame_p95_ms']
        quality[key] = {'participants': len(cohort['participants']), 'reported_finished_runs': cohort['finished_runs'],
            'runs_with_machine': sum(n > 0 for n in machines),
            'mean_machines_per_reported_run': sum(machines) / len(machines) if machines else None,
            'reported_p95_ms_range': [min(frames), max(frames)] if frames else None,
            'replay_interest_responses': dict(cohort['replay_interest_responses'])}
    robot_summary = {key: {name: {'observed_events': len(values), 'median_seconds': statistics.median(values), 'range_seconds': [min(values), max(values)]} for name, values in samples.items()} for key, samples in robot_samples.items()}
    return {'robot_milestones_by_build_platform_stage': robot_summary, 'robot_discovery_events': {key: dict(values) for key, values in discovery.items()}, 'combat_quality_by_build_platform': quality, 'participants': len(days), 'excluded_test_events': excluded,
            'events': dict(counts), 'interested_participants': {k: len(v) for k, v in groups.items() if 'interest:' in k},
            'gems_earned': gems_in, 'gems_spent': gems_out,
            'battle_starts': dict(starts), 'battle_ends': dict(ends),
            'observed_retention': retention,
            'limits': 'Robot milestone times are observed event samples, not completion rates; missing samples do not prove failure. parts_ready means any uncrafted design, not a fixed bot target; observed at approximately 0.25-second intervals. An unprompted workshop open does not prove independent button discovery. Opt-in sample; first observed day is not installation day. 14-day local retention, voluntary exports and consent changes can bias results. Interest is not a cash purchase; cosmetic_purchase uses game gems. Unfinished battles are not proven quits. Replay-interest responses are intentions, not measured retention; per-run p95 is capped at 250 ms and is not a population percentile.'}

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
