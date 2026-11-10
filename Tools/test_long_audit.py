import copy,gzip,json,unittest
from pathlib import Path
from summarize_long_audit import summarize,completion_minutes,last_week
def fixture():return json.loads(gzip.decompress((Path(__file__).resolve().parents[1]/'docs/reports/long033-pilot.json.gz').read_bytes()))
class LongAuditTests(unittest.TestCase):
    def test_new_schema_separates_finale(self):
        d=fixture();d["schema"]=2;d["campaignCount"]=60
        for j in d["journeys"]:j["firstCampaignDay"]=0
        s=summarize(d);self.assertEqual(s["campaignCount"],60);self.assertEqual(s["finalCompletions"],0)
        d["journeys"][0]["firstCampaignDay"]=1
        with self.assertRaisesRegex(ValueError,"campaign completion"):summarize(d)
    def test_real_pilot_and_censoring(self):
        s=summarize(fixture());self.assertEqual((s['journeys'],s['battles']),(8,96))
        self.assertTrue(all(x['daysSaved'] is None for x in s['pairs']))
    def test_missing_and_duplicate_paths(self):
        for duplicate in (True,False):
            d=fixture()
            if duplicate:d['journeys'].append(copy.deepcopy(d['journeys'][0]))
            else:d['journeys'].pop()
            with self.assertRaises(ValueError):summarize(d)
    def test_wallet_injection(self):
        d=fixture();d['journeys'][0]['ledger'][0]['after']['gold']+=100
        with self.assertRaisesRegex(ValueError,'delta'):summarize(d)
    def test_unearned_salvage(self):
        d=fixture();r=d['journeys'][0]['ledger'][0];r['action']='salvage_claim'
        with self.assertRaisesRegex(ValueError,'unearned salvage'):summarize(d)
    def test_unknown_free_money_source(self):
        d=fixture();d['journeys'][0]['ledger'][0]['action']='fake_grant'
        with self.assertRaisesRegex(ValueError,'unknown source'):summarize(d)
    def test_future_stage(self):
        d=fixture();d['journeys'][0]['runs'][0]['stage']=42
        with self.assertRaisesRegex(ValueError,'future progression'):summarize(d)
    def test_payment_totals_and_seed_sets(self):
        d=fixture();d['journeys'][0]['paidSpent']+=1
        with self.assertRaisesRegex(ValueError,'totals'):summarize(d)
        d=fixture();d['seeds']=10
        with self.assertRaisesRegex(ValueError,'unpaired seeds'):summarize(d)
    def test_fake_completion(self):
        d=fixture();d['journeys'][0]['first42Day']=1
        with self.assertRaisesRegex(ValueError,'completion/censoring'):summarize(d)
    def test_completion_time_excludes_same_day_repeats_after_clear(self):
        def run(stage,won,repeat=False):return dict(stage=stage,won=won,repeat=repeat,mode='',seconds=150)
        j={'runs':[run(42,False),run(3,True,True),run(42,True),run(3,True,True)]}
        self.assertEqual(completion_minutes(j),9)
        self.assertIsNone(completion_minutes({'runs':j['runs'][:2]}))
    def test_daily_snapshot_cannot_fake_remaining_gems(self):
        d=fixture();d['journeys'][0]['days'][0]['state']['wallet']['gems']+=100
        with self.assertRaisesRegex(ValueError,'daily wallet snapshot'):summarize(d)
    def test_last_week_excludes_paid_balance_and_first_day(self):
        j={'days':[{'state':{'wallet':{'gems':100}},'paid':50,'cleared':42} for _ in range(8)],
           'ledger':[{'serviceDate':'2026-11-09','action':'buy_kit'},{'serviceDate':'2026-11-10','action':'buy_kit'}]}
        j['days'][-1]={'state':{'wallet':{'gems':80}},'paid':5,'cleared':42}
        self.assertEqual(last_week(j,8),dict(freeChange=25,kits=1,completedBefore=True))
        self.assertIsNone(last_week(j,7))
if __name__=='__main__':unittest.main()
