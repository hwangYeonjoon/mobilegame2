#!/usr/bin/env python3
"""Synthetic accounting fixtures only; never participant or combat evidence."""
import copy
import unittest
from summarize_growth_audit import summarize, STRATEGIES, BENEFITS, POLICIES


def fixture():
    wallet = dict(gold=1800, parts=30, gems=0)
    before = dict(wallet=wallet, unlockedStage=1, clearedStages=[], totalRuns=0,
                  attack=130, health=1180, heroLevel=1,
                  equipped=[dict(level=1, rarity=0, definition=i) for i in range(3)])
    after = dict(before, totalRuns=1)
    run = dict(stage=1, attempt=1, sequence=1, before=before, after=after,
               won=False, reward=dict(gold=0, parts=0, gems=0),
               preparation=dict(visited=False, nextUpgrade=dict(canAfford=True, missingGold=0, missingParts=0)),
               bossReached=False, cause='Contact', bossSeconds=0,
               damageReceived=1180, bossContactDamage=0, bossWarningDamage=0)
    ledger = [dict(sequence=1, stage=1, attempt=1, action='battle_finish', before=wallet,
                   after=wallet, delta=dict(gold=0, parts=0, gems=0))]
    ledger.insert(0, dict(ledger[0], action='battle_start'))
    ledger[1]['sequence'] = 2
    return dict(kind='economic_trajectory_simulation_not_human', schema=1, build=34, rules=22,
                seedCount=1, maxStage=2, maxAttempts=1, assumptions=['Synthetic unit fixture'],
                journeys=[copy.deepcopy(dict(seed=17, strategy=s, benefits=b, policy=p,
                    maxStage=2, maxAttempts=1, attempts=[run], ledger=ledger, final=after,
                    clearedThrough=0, stoppedAt=1, status='retry_limit', completedRuns=1))
                    for s in STRATEGIES for b in BENEFITS for p in POLICIES])


class GrowthReportTests(unittest.TestCase):
    def test_unreached_never_becomes_zero_equipment_or_loss(self):
        result = summarize(fixture())
        self.assertEqual(result['journeyCount'], 27)
        self.assertEqual(result['runCount'], 27)
        for row in result['rows']:
            if row['stage'] == 2:
                self.assertIsNone(row['entryAttack'])
                self.assertIsNone(row['clearedPerReached'])
                self.assertEqual(row['unreached'], 1)
                self.assertEqual(row['attempts'], 0)
            else:
                self.assertEqual(row['deferredAffordable'], 1)

    def test_duplicate_journey(self):
        data=fixture(); data['journeys'].append(copy.deepcopy(data['journeys'][0]))
        with self.assertRaisesRegex(ValueError, 'Duplicate'): summarize(data)

    def test_missing_cohort(self):
        data=fixture(); data['journeys'].pop()
        with self.assertRaisesRegex(ValueError, 'Missing cohort'): summarize(data)

    def test_unpaired_seed(self):
        data=fixture(); data['journeys'][0]['seed']=18
        with self.assertRaisesRegex(ValueError, 'Unpaired'): summarize(data)

    def test_wallet_injection(self):
        data=fixture(); data['journeys'][0]['ledger'][0]['delta']['gold']=100
        with self.assertRaisesRegex(ValueError, 'reconcile'): summarize(data)

    def test_wrong_reward(self):
        data=fixture(); data['journeys'][0]['attempts'][0]['reward']['gold']=100
        with self.assertRaisesRegex(ValueError, 'Receipt'): summarize(data)

    def test_skipped_stage(self):
        data=fixture(); data['journeys'][0]['attempts'][0]['stage']=2
        with self.assertRaisesRegex(ValueError, 'Skipped'): summarize(data)

    def test_future_unlock(self):
        data=fixture(); data['journeys'][0]['attempts'][0]['before']['unlockedStage']=2
        with self.assertRaisesRegex(ValueError, 'Future'): summarize(data)

    def test_truncated_retry_budget(self):
        data=fixture(); data['maxAttempts']=2
        for j in data['journeys']: j['maxAttempts']=2
        with self.assertRaisesRegex(ValueError, 'Truncated'): summarize(data)


if __name__ == '__main__':
    unittest.main()
