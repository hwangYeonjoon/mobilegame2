import copy
import gzip
import json
from pathlib import Path
import unittest
import tempfile
from summarize_purchase_audit import summarize, validate, merge_audits

ROOT=Path(__file__).resolve().parents[1]
def fixture():return json.loads(gzip.decompress((ROOT/'docs/reports/purchase032-pilot.json.gz').read_bytes()))

class PurchaseAuditTests(unittest.TestCase):
    def test_actual_pilot_and_common_free_rewards(self):
        d=fixture();r=summarize(d)
        self.assertEqual((r['journeys'],r['battles']),(16,96))
        for j in d['journeys']:
            self.assertEqual(sum(x['action']=='pass_free_claim' for x in j['ledger']),2)
            if not j['cohort'].endswith('_paid'):self.assertEqual(j['paidGranted'],0)
        for row in r['paired']:
            if row['outcome']=='both_cleared':self.assertIsNotNone(row['sortiesSaved'])
    def test_duplicate_rejected(self):
        d=fixture();d['journeys'].append(copy.deepcopy(d['journeys'][0]))
        with self.assertRaisesRegex(ValueError,'Duplicate journey'):validate(d)
    def test_missing_cohort_rejected(self):
        d=fixture();d['journeys'].pop()
        with self.assertRaisesRegex(ValueError,'Missing cohort'):validate(d)
    def test_unpaired_seed_rejected(self):
        d=fixture();d['seedCount']=2
        with self.assertRaisesRegex(ValueError,'Missing/unpaired'):validate(d)
    def test_ledger_injection_rejected(self):
        d=fixture();d['journeys'][0]['ledger'][0]['delta']['gold']+=1
        with self.assertRaisesRegex(ValueError,'delta mismatch'):validate(d)
    def test_paid_provenance_rejected(self):
        d=fixture();j=next(j for j in d['journeys'] if j['cohort']=='kit_paid');row=next(x for x in j['ledger'] if x['action']=='simulated_topup');row['paidAfter']-=1
        with self.assertRaisesRegex(ValueError,'Wrong pack grant'):validate(d)
    def test_fake_totals_rejected(self):
        d=fixture();d['journeys'][0]['paidSpent']=100
        with self.assertRaisesRegex(ValueError,'Funding totals'):validate(d)
    def test_future_rewards_rejected(self):
        d=fixture();row=d['journeys'][0]['ledger'][0];row['action']='pass_free_claim';row['target']='3'
        with self.assertRaisesRegex(ValueError,'Unearned pass'):validate(d)
    def test_unmatched_receipt_rejected(self):
        d=fixture();row=next(x for x in d['journeys'][0]['ledger'] if x['action']=='battle_finish');row['target']='different'
        with self.assertRaisesRegex(ValueError,'Unmatched settlement'):validate(d)
    def test_seed_formula_rejected(self):
        d=fixture();d['journeys'][0]['runs'][0]['battle']['seed']+=1
        with self.assertRaisesRegex(ValueError,'Unpaired combat'):validate(d)
    def test_fake_farm_rejected(self):
        d=fixture();d['journeys'][0]['runs'][0]['farm']=True
        with self.assertRaisesRegex(ValueError,'invalid farm'):validate(d)
    def test_calendar_truncation_rejected(self):
        d=fixture();d['journeys'][0]['days'].pop()
        with self.assertRaisesRegex(ValueError,'Missing calendar'):validate(d)
    def test_wrong_play_time_rejected(self):
        d=fixture();d['journeys'][0]['days'][0]['activeSeconds']+=300
        with self.assertRaisesRegex(ValueError,'Play time'):validate(d)
    def test_final_run_claims_counted(self):
        d=fixture();ads=next(j for j in d['journeys'] if j['cohort']=='ads_only')
        self.assertEqual(ads['adsViewed'],5)
        self.assertEqual(ads['days'][0]['ledgerEnd'],len(ads['ledger']))
    def test_wrong_price_rejected(self):
        d=fixture();j=next(j for j in d['journeys'] if j['cohort']=='both_paid');j['proposedWon']-=100
        with self.assertRaisesRegex(ValueError,'Funding totals'):validate(d)
    def test_oversized_topup_rejected_even_with_matching_grant(self):
        d=fixture();j=next(j for j in d['journeys'] if j['cohort']=='kit_paid');r=next(x for x in j['ledger'] if x['action']=='simulated_topup')
        r['target']='gems_600';r['delta']['gems']=600;r['after']['gems']+=300;r['paidAfter']+=300
        with self.assertRaisesRegex(ValueError,'oversized top-up'):validate(d)
    def test_attendance_cannot_mint_extra_income(self):
        d=fixture();r=d['journeys'][0]['ledger'][0];self.assertEqual(r['action'],'attendance')
        r['delta']['gold']+=100;r['after']['gold']+=100
        with self.assertRaisesRegex(ValueError,'Wrong attendance'):validate(d)
    def test_disjoint_merge_and_guards(self):
        d=fixture()
        with tempfile.TemporaryDirectory() as folder:
            paths=[Path(folder)/'a.json',Path(folder)/'b.json']
            for path,cohorts in zip(paths,[d['cohorts'][:4],d['cohorts'][4:]]):
                part=copy.deepcopy(d);part['cohorts']=cohorts;part['journeys']=[j for j in part['journeys'] if j['cohort'] in cohorts]
                path.write_text(json.dumps(part))
            merged=merge_audits(paths)
            self.assertEqual(len(merged['journeys']),16)
            self.assertEqual(len(merged['sources']),2)
            with self.assertRaisesRegex(ValueError,'Overlapping'):merge_audits([paths[0],paths[0]])
            part=json.loads(paths[1].read_text());part['build']+=1;paths[1].write_text(json.dumps(part))
            with self.assertRaisesRegex(ValueError,'Mixed audit'):merge_audits(paths)
    def test_unknown_or_duplicate_selection_rejected(self):
        for names in [('none','none'),('unknown',)]:
            d=fixture();d['cohorts']=names
            with self.assertRaisesRegex(ValueError,'Unknown cohorts'):validate(d)
    def test_completed_audit_keeps_censored_values_missing(self):
        d=json.loads(gzip.decompress((ROOT/'docs/reports/purchase032-calendar.json.gz').read_bytes()));r=summarize(d)
        self.assertEqual((r['journeys'],r['battles']),(54,3543))
        absent=[s for s in r['stages'] if not s['reached']]
        self.assertTrue(absent)
        for s in absent:
            self.assertIsNone(s['entryAttack']);self.assertIsNone(s['entryHealth']);self.assertIsNone(s['clearDay'])
        missing=[x for x in r['paired'] if x['outcome']!='both_cleared'];self.assertTrue(missing)
        for x in missing:self.assertIsNone(x['sortiesSaved']);self.assertIsNone(x['daysSaved'])
        self.assertEqual(len([x for x in r['calendarPairs'] if x['cohort']=='both_paid' and x['baseline']=='both_free_kit_first']),84)

if __name__=='__main__':unittest.main()
