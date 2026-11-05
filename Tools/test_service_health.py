import os
import sqlite3
import tempfile
import unittest
from pathlib import Path

from service_health import inspect_backup, service_ok


class BackupHealthTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.snapshot = self.root / 'scrapguard-20261105T000000Z-1.db'
        with sqlite3.connect(self.snapshot) as db:
            db.executescript('''
                CREATE TABLE accounts(id TEXT PRIMARY KEY, revision INTEGER);
                CREATE TABLE purchases(owner TEXT REFERENCES accounts(id), token_cipher TEXT);
                CREATE TABLE refund_inbox(cipher TEXT);
            ''')
        self.now = self.snapshot.stat().st_mtime

    def check_snapshot(self):
        return inspect_backup(self.root, now=self.now)

    def test_empty_but_valid_database_without_receipts(self):
        self.assertTrue(self.check_snapshot()['ok'])

    def test_corrupt_newest_snapshot_does_not_fall_back_to_old_valid_copy(self):
        broken = self.root / 'scrapguard-new.db'
        broken.write_bytes(b'not a sqlite database')
        os.utime(broken, (self.now + 1, self.now + 1))
        self.assertEqual('snapshot_unreadable', self.check_snapshot()['reason'])

    def test_stale_snapshot_cannot_be_masked_by_unrelated_database(self):
        os.utime(self.snapshot, (self.now - 27 * 3600,) * 2)
        (self.root / 'unrelated.db').write_bytes(b'new')
        self.assertEqual('stale_snapshot', self.check_snapshot()['reason'])

    def test_future_timestamp_is_not_healthy(self):
        os.utime(self.snapshot, (self.now + 3600,) * 2)
        self.assertEqual('future_timestamp', self.check_snapshot()['reason'])

    def test_purchase_and_inbox_ciphertexts_both_require_keys(self):
        for table, column in [('purchases', 'token_cipher'), ('refund_inbox', 'cipher')]:
            with self.subTest(table=table):
                with sqlite3.connect(self.snapshot) as db:
                    db.execute(f'INSERT INTO {table}({column}) VALUES (?)', ('private-test-token',))
                self.assertEqual('missing_receipt_keys', self.check_snapshot()['reason'])
                with sqlite3.connect(self.snapshot) as db:
                    db.execute(f'DELETE FROM {table}')

    def test_key_presence_is_distinct_from_decryption_verification(self):
        with sqlite3.connect(self.snapshot) as db:
            db.execute("INSERT INTO purchases(token_cipher) VALUES ('cipher')")
        keys = Path(str(self.snapshot) + '.keys')
        keys.mkdir()
        (keys / 'key.xml').touch()
        self.assertFalse(self.check_snapshot()['ok'])
        (keys / 'key.xml').write_text('<key />')
        result = self.check_snapshot()
        self.assertTrue(result['ok'])
        self.assertIn('decryption/restore not verified', result['scope'])

    def test_foreign_key_damage_is_detected(self):
        with sqlite3.connect(self.snapshot) as db:
            db.execute("INSERT INTO purchases(owner) VALUES ('missing-account')")
        self.assertEqual('foreign_key_failed', self.check_snapshot()['reason'])

    def test_missing_directory_does_not_create_backup(self):
        self.assertEqual('missing_snapshot', inspect_backup(self.root / 'absent')['reason'])
        self.assertFalse((self.root / 'absent').exists())


class ServiceHealthTests(unittest.TestCase):
    def test_http_success_alone_does_not_mean_service_is_healthy(self):
        for name in ('api', 'database', 'services'):
            for data in ({}, [], None, {'status': 'unavailable'}):
                self.assertFalse(service_ok(name, data))
        self.assertTrue(service_ok('api', {'status': 'ok', 'maintenance': False}))
        self.assertFalse(service_ok('api', {'status': 'ok', 'maintenance': True}))
        self.assertTrue(service_ok('database', {'status': 'ready'}))

    def test_either_refund_path_can_raise_attention(self):
        for polling in (False, True):
            for recovery in (False, True):
                data = {'refunds': {'needsAttention': polling},
                        'refundRecovery': {'needsAttention': recovery}}
                self.assertEqual(not (polling or recovery), service_ok('services', data))


if __name__ == '__main__':
    unittest.main()
