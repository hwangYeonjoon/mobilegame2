#!/usr/bin/env python3
"""Read-only service check. Emits JSON and nonzero exit status for a scheduler/monitor."""
import argparse
import json
import math
import sqlite3
import time
from pathlib import Path
from urllib.parse import urlparse
from urllib.request import urlopen

def inspect_backup(directory, max_hours=26, now=None):
    """Inspect the latest exported snapshot without opening the live database."""
    result = {'ok': False, 'scope': 'local snapshot integrity and key presence only; off-host copy and decryption/restore not verified'}
    try:
        snapshots = list(directory.glob('scrapguard-*.db'))
        if not snapshots:
            return {**result, 'reason': 'missing_snapshot'}
        latest = max(snapshots, key=lambda path: path.stat().st_mtime)
        age = ((time.time() if now is None else now) - latest.stat().st_mtime) / 3600
        result['age_hours'] = age
        if age < -5 / 60 or age > max_hours:
            return {**result, 'reason': 'future_timestamp' if age < 0 else 'stale_snapshot'}
        if latest.is_symlink() or latest.stat().st_size == 0:
            return {**result, 'reason': 'invalid_snapshot'}
        # mode=ro avoids creating a replacement when a file disappears mid-check.
        db = sqlite3.connect(latest.resolve().as_uri() + '?mode=ro', uri=True, timeout=5)
        try:
            if db.execute('PRAGMA quick_check').fetchall() != [('ok',)]:
                return {**result, 'reason': 'integrity_failed'}
            if db.execute('PRAGMA foreign_key_check').fetchone():
                return {**result, 'reason': 'foreign_key_failed'}
            db.execute('SELECT revision FROM accounts LIMIT 1').fetchone()
            needs_keys = bool(db.execute("SELECT 1 FROM purchases WHERE token_cipher IS NOT NULL AND token_cipher <> '' LIMIT 1").fetchone())
            needs_keys |= bool(db.execute("SELECT 1 FROM refund_inbox WHERE cipher IS NOT NULL AND cipher <> '' LIMIT 1").fetchone())
        finally:
            db.close()
        if needs_keys:
            keys = Path(str(latest) + '.keys')
            if keys.is_symlink() or not any(p.is_file() and not p.is_symlink() and p.stat().st_size > 0 for p in keys.glob('*.xml')):
                return {**result, 'reason': 'missing_receipt_keys'}
        return {**result, 'ok': True, 'receipt_keys_required': needs_keys}
    except (OSError, sqlite3.Error) as error:
        # Never include database contents, paths or receipt tokens in monitor output.
        return {**result, 'reason': 'snapshot_unreadable', 'error_type': type(error).__name__}

def service_ok(name, data):
    if not isinstance(data, dict):
        return False
    if name == 'api':
        return data.get('status') == 'ok' and data.get('maintenance') is False
    if name == 'database':
        return data.get('status') == 'ready'
    return all(isinstance(data.get(key), dict) and data[key].get('needsAttention') is False
               for key in ('refunds', 'refundRecovery'))

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--url',default='http://127.0.0.1:5190')
    p.add_argument('--backup-dir',type=Path)
    p.add_argument('--max-backup-hours',type=float,default=26)
    a=p.parse_args();u=urlparse(a.url)
    if not math.isfinite(a.max_backup_hours) or a.max_backup_hours <= 0:
        p.error('--max-backup-hours must be a finite positive number.')
    if u.username or u.password or u.query or u.fragment or not (u.scheme=='https' or u.scheme=='http' and u.hostname in ('127.0.0.1','localhost')):
        p.error('Use HTTPS or loopback, without credentials or query strings.')
    checks={}
    for name,path in [('api','/health'),('database','/health/ready'),('services','/health/services')]:
        try:
            with urlopen(a.url.rstrip('/')+path,timeout=10) as response:data=json.load(response)
            checks[name]={'ok': service_ok(name, data),'details':data}
        except Exception as e: checks[name]={'ok':False,'error_type':type(e).__name__}
    if a.backup_dir:
        checks['backup']=inspect_backup(a.backup_dir,a.max_backup_hours)
    print(json.dumps({'ok':all(c['ok'] for c in checks.values()),'checks':checks},indent=2))
    return 0 if all(c['ok'] for c in checks.values()) else 2
if __name__=='__main__':raise SystemExit(main())
