#!/usr/bin/env python3
"""Read-only service check. Emits JSON and nonzero exit status for a scheduler/monitor."""
import argparse
import json
import time
from pathlib import Path
from urllib.parse import urlparse
from urllib.request import urlopen

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--url',default='http://127.0.0.1:5190')
    p.add_argument('--backup-dir',type=Path)
    p.add_argument('--max-backup-hours',type=float,default=26)
    a=p.parse_args();u=urlparse(a.url)
    if u.username or u.password or u.query or u.fragment or not (u.scheme=='https' or u.scheme=='http' and u.hostname in ('127.0.0.1','localhost')):
        p.error('Use HTTPS or loopback, without credentials or query strings.')
    checks={}
    for name,path in [('api','/health'),('database','/health/ready'),('services','/health/services')]:
        try:
            with urlopen(a.url.rstrip('/')+path,timeout=10) as response:data=json.load(response)
            checks[name]={'ok': not data.get('refunds',{}).get('needsAttention',False),'details':data}
        except Exception as e: checks[name]={'ok':False,'error_type':type(e).__name__}
    if a.backup_dir:
        backups=list(a.backup_dir.glob('*.db'))
        age=(time.time()-max(p.stat().st_mtime for p in backups))/3600 if backups else None
        checks['backup']={'ok':age is not None and age<=a.max_backup_hours,'age_hours':age,'scope':'local exported snapshot; off-host copy and restore not verified'}
    print(json.dumps({'ok':all(c['ok'] for c in checks.values()),'checks':checks},indent=2))
    return 0 if all(c['ok'] for c in checks.values()) else 2
if __name__=='__main__':raise SystemExit(main())
