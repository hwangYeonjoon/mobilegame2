import json
import sqlite3
import tempfile
import unittest
from datetime import datetime, timezone
from pathlib import Path

from launch_dashboard import aggregate, render, private_write


class LaunchDashboardTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name)/'game.db'
        with sqlite3.connect(self.path) as db:
            db.executescript('''
                CREATE TABLE accounts(id,token_hash,state,created);
                CREATE TABLE metrics_meta(id,started);
                CREATE TABLE metrics_activity(owner,day);
                CREATE TABLE purchases(owner,product,revoked,is_test,created);
                CREATE TABLE ad_claims(owner,status,test,transaction_id,created);
                CREATE TABLE ledger(owner,type,delta,currency,created);
                INSERT INTO metrics_meta VALUES(1,'2026-11-01');
            ''')
            for owner, test in [('real', False), ('qa', True), ('review', False)]:
                db.execute('INSERT INTO accounts VALUES(?,?,?,?)', (owner, 'hash-'+owner, json.dumps({'testProgress':test}), '2026-11-01T23:00:00Z'))
            db.executescript('''
                INSERT INTO accounts VALUES('new','hash-new','{}','2026-11-08T01:00:00Z');
                INSERT INTO metrics_activity VALUES('real','2026-11-02'),('real','2026-11-02'),('real','2026-11-08'),('qa','2026-11-02');
                INSERT INTO purchases VALUES('real','gems_300',1,0,'2026-11-02'),('real','gems_300',0,1,'2026-11-02'),('real','gems_300',0,NULL,'2026-11-02'),('review','gems_300',0,0,'2026-11-02');
                INSERT INTO ad_claims VALUES('real','granted',0,'skip:123','2026-11-02'),('real','granted',0,'ssv123','2026-11-02'),('real','granted',1,'test','2026-11-02');
                INSERT INTO ledger VALUES('real','play_refund',-300,'gems','2026-11-02');
            ''')

    def collect(self, day=8):
        return aggregate(self.path, '2026-11-01', {'ids':['review'],'hashes':[]}, datetime(2026,11,day,12,tzinfo=timezone.utc))

    def test_cohort_matures_only_after_target_day_finishes(self):
        d = self.collect()
        self.assertEqual(1, d['cohorts'][0]['d1'])
        self.assertIsNone(d['cohorts'][0]['d7'])
        self.assertEqual(1, self.collect(9)['cohorts'][0]['d7'])
        self.assertIsNone(d['cohorts'][-1]['d1'])
        self.assertEqual(1, d['daily'][1]['commandActive'])

    def test_exclusions_test_unknown_refunds_and_ad_skip(self):
        d = self.collect()
        self.assertEqual(2, d['accounts'])
        self.assertEqual(2, d['excludedAccounts'])
        self.assertEqual(1, d['purchases']['verified'])
        self.assertEqual(1, d['purchases']['revoked'])
        self.assertEqual(1, d['purchases']['unclassified'])
        self.assertEqual(1, d['purchases']['testExcluded'])
        self.assertEqual({'skipRights':1,'verifiedRewards':1}, d['ads'])
        self.assertEqual(300, d['gems']['play_refund']['spent'])
        output = json.dumps(d)
        self.assertNotIn('hash-real', output)
        self.assertNotIn('ssv123', output)

    def test_database_not_changed_and_missing_db_not_created(self):
        original = self.path.read_bytes()
        self.collect()
        self.assertEqual(original, self.path.read_bytes())
        missing = self.path.with_name('missing.db')
        with self.assertRaises(sqlite3.OperationalError):
            aggregate(missing, '2026-11-01', now=datetime(2026,11,8,tzinfo=timezone.utc))
        self.assertFalse(missing.exists())

    def test_html_escaped_and_private_atomic_output(self):
        d = self.collect()
        d['gems']['<script>alert(1)</script>'] = {'gain':0,'spent':0,'events':0}
        page = render(d)
        self.assertNotIn('<script>alert', page)
        self.assertIn('&lt;script&gt;', page)
        output = self.path.parent/'private'/'index.html'
        private_write(output, page)
        self.assertEqual(0o600, output.stat().st_mode & 0o777)
        self.assertEqual(0o700, output.parent.stat().st_mode & 0o777)


if __name__ == '__main__':
    unittest.main()
