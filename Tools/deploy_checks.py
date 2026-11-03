"""Exercise only the isolated loopback Docker stack; never contacts a public server."""
import json
import secrets
import subprocess
import time
import urllib.request
import urllib.error
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMPOSE = ['docker', 'compose', '-f', str(ROOT / 'Server/Deploy/compose.local.yaml')]
BASE = 'http://127.0.0.1:5190'
checks = []
secret = secrets.token_urlsafe(32)

def check(value, name):
    assert value, name
    checks.append(name)
    print('PASS', name, flush=True)

def request(path, data=None, token=None, build="15", rules="11"):
    headers = {'Content-Type': 'application/json'}
    if build is not None:
        headers['X-Scrap-Build'] = build
    if rules is not None:
        headers['X-Scrap-Rules'] = rules
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(BASE + path, data=None if data is None else json.dumps(data).encode(), headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=10) as reply:
            return reply.status, json.load(reply)
    except urllib.error.HTTPError as error:
        return error.code, json.load(error)

def command(*args):
    return subprocess.run(COMPOSE + list(args), cwd=ROOT, check=True, capture_output=True, text=True).stdout

check(request('/health')[1]['version'] == '0.15.0', 'container health version')
check(request('/health/ready')[0] == 200, 'database readiness')
status, release = request('/v1/release', build=None, rules=None)
check(status == 200 and release['minimumBuild'] == 14 and release['latestBuild'] == 15 and not release['playAvailable'], 'public release policy is reachable without client headers')
for build in [None, '13', '-1', '14,15']:
    status, reply = request('/v1/bootstrap', build=build)
    check(status == 426 and reply['code'] == 'client_update_required' and reply['release']['minimumBuild'] == 14, 'unsupported client receives structured update policy ' + str(build))
check(request('/v1/bootstrap', rules='10')[0] == 426, 'incompatible client rules rejected by HTTP gate')

check(request('/v1/auth/guest', {'secret': secret})[0] == 403, 'HTTP invitation guard')
status, initial = request('/v1/auth/guest', {'secret': secret, 'inviteCode': 'local-integration-test-invite-03'})
check(status == 200 and initial['success'], 'invited account registration')
check(request('/v1/commerce', token=secret)[1]['licenseTest'] is False, 'live checkout disabled by default')
check(request('/v1/commerce/verify', {'productId':'gems_600','purchaseToken':'fake'}, secret)[0] == 503, 'disabled purchase endpoint cannot credit')
check(request('/v1/auth/google/challenge', {'link':True}, secret)[0] == 503, 'missing Google configuration fails closed')
check(request('/v1/skill', token=secret)[1]['entries'] == [], 'new skill board starts empty')
attend = {'type':'attendance', 'requestId':secrets.token_hex(16), 'revision':initial['revision'], 'rulesVersion':4}

check(request('/v1/commands', attend, secret, build='13')[0] == 426, 'required update rejects mutation')
check(request('/v1/bootstrap', token=secret)[1]['revision'] == initial['revision'], 'blocked update request leaves account untouched')
status, state = request('/v1/commands', attend, secret)
check(status == 200 and state['state']['attendanceCount'] == 1, 'HTTP economy transaction')
check(request('/v1/commands', attend, secret)[1]['revision'] == state['revision'], 'HTTP retry idempotency')
backup = '/backups/deploy-check-' + secrets.token_hex(8) + '.db'
check('BACKUP_VERIFIED' in command('exec','-T','api','dotnet','ScrapGuard.Server.dll','--backup-database',backup), 'running container online backup')
check('DATABASE_VERIFIED' in command('exec','-T','api','dotnet','ScrapGuard.Server.dll','--verify-backup',backup), 'snapshot integrity verification')
output = ROOT/'TestResults/deploy-backup-check.db'
command('cp','api:'+backup,str(output))
import sqlite3
with sqlite3.connect(output) as db:
    row = db.execute('SELECT state, revision FROM accounts WHERE id=?',(initial['playerId'],)).fetchone()
    check(row and json.loads(row[0])['gold'] == state['state']['gold'] and row[1] == state['revision'], 'exported snapshot restores matching wallet and revision')
command('restart','api')
for attempt in range(30):
    try:
        if request('/health/ready')[0] == 200:
            break
    except (OSError, ValueError):
        pass
    time.sleep(.3)
restored = request('/v1/bootstrap', token=secret)[1]
check(restored['playerId'] == initial['playerId'] and restored['state']['gold'] == state['state']['gold'], 'container restart retains volume and account')
(ROOT/'TestResults/deploy-015-checks.json').write_text(json.dumps({'passed':len(checks), 'checks':checks, 'scope':'isolated loopback Docker, no public TLS'},indent=2)+'\n')
print(len(checks), 'deployment checks passed')
