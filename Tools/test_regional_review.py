import copy
import unittest
import regional_review as review

class RegionalReviewTests(unittest.TestCase):
    def setUp(self):
        self.profiles = [dict(stage=s, policy=p, seed=n, snapshot=dict(attack=100, health=1000))
                         for s in review.STAGES for p in review.POLICIES for n in review.SEEDS]
        self.rows = [dict(stage=p['stage'], policy=p['policy'], seed=p['seed'], attack=100, health=1000,
                          mission='Survive', Won=True, DamageReceived=100, damage=[0, 25, 25, 25, 0, 25],
                          healing=[0]*7, LowHealthSeconds=0, Elapsed=180, bossReached=False) for p in self.profiles]
        self.inputs = dict(profiles=self.profiles)
        self.before = dict(build=39, rules=26, rows=self.rows)
        self.after = copy.deepcopy(dict(build=40, rules=27, rows=self.rows))

    def test_order_does_not_change_pairs(self):
        expected = review.report(self.inputs, self.before, self.after)
        self.after['rows'].reverse()
        self.assertEqual(expected, review.report(self.inputs, self.before, self.after))

    def test_missing_or_duplicate_seed_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'incomplete'):
            review.indexed(self.rows[:-1])
        with self.assertRaisesRegex(ValueError, 'duplicate'):
            review.indexed(self.rows + self.rows[:1])

    def test_gear_mismatch_cannot_be_presented_as_balance_change(self):
        self.after['rows'][0]['attack'] += 1
        with self.assertRaisesRegex(ValueError, 'gear mismatch'):
            review.report(self.inputs, self.before, self.after)

    def test_damage_sources_must_reconcile(self):
        self.after['rows'][0]['damage'][3] += 20
        with self.assertRaisesRegex(ValueError, 'source sum mismatch'):
            review.report(self.inputs, self.before, self.after)

if __name__ == '__main__':
    unittest.main()
