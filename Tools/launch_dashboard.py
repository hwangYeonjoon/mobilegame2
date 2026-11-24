#!/usr/bin/env python3
"""Private launch dashboard. Aggregate on the server; never download player rows."""
import argparse
from collections import Counter, defaultdict
from datetime import date, datetime, timedelta, timezone
import hashlib
import html
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
PLAY = 'https://play.google.com/console/u/1/developers/8898884610263017382/app/4972707747613434942'


def aggregate(path, since, exclusions=None, now=None):
    now = now or datetime.now(timezone.utc)
    today = now.date()
    start = date.fromisoformat(since)
    if not today - timedelta(days=83) <= start <= today:
        raise ValueError('Use a start date within 83 days (activity retention is 90 days).')
    excluded = exclusions or {'ids': [], 'hashes': []}
    db = sqlite3.connect(Path(path).resolve().as_uri() + '?mode=ro', uri=True, timeout=10)
    try:
        db.execute('PRAGMA query_only=ON')
        db.execute('BEGIN')
        captured = db.execute('SELECT date(started) FROM metrics_meta WHERE id=1').fetchone()[0]
        start = max(start, date.fromisoformat(captured))
        accounts = {}
        ignored = 0
        for owner, token_hash, state, created in db.execute('SELECT id,token_hash,state,date(created) FROM accounts'):
            s = json.loads(state)
            if s.get('testProgress') or owner in excluded['ids'] or token_hash in excluded['hashes']:
                ignored += 1
                continue
            accounts[owner] = {'created': created, 'stage': int(s.get('unlockedStage', 0)) + 1,
                               'intro': bool(s.get('guideIntroSeen'))}
        activity = defaultdict(set)
        for owner, day in db.execute('SELECT owner,day FROM metrics_activity WHERE day>=? AND day<=?', (str(start), str(today))):
            if owner in accounts:
                activity[day].add(owner)
        daily = []
        cohorts = []
        for offset in range(max(0, (today - start).days + 1)):
            day = start + timedelta(days=offset)
            cohort = {k for k, v in accounts.items() if v['created'] == str(day)}
            row = {'date': str(day), 'newAccounts': len(cohort), 'commandActive': len(activity[str(day)]),
                   'partial': day == today}
            daily.append(row)
            retention = {'date': str(day), 'accounts': len(cohort)}
            for lag in (1, 7):
                target = day + timedelta(days=lag)
                retention['d' + str(lag)] = (len(cohort & activity[str(target)])
                                            if target < today and cohort else None)
            cohorts.append(retention)
        purchases = Counter()
        payers = set()
        for owner, product, revoked, test in db.execute(
                'SELECT owner,product,revoked,is_test FROM purchases WHERE date(created)>=? AND date(created)<=?',
                (str(start), str(today))):
            if owner not in accounts:
                continue
            if test is None:
                purchases['unclassified'] += 1
            elif test:
                purchases['testExcluded'] += 1
            else:
                purchases['verified'] += 1
                purchases['revoked'] += bool(revoked)
                purchases[product] += 1
                payers.add(owner)
        ads = Counter()
        for owner, status, test, transaction in db.execute(
                'SELECT owner,status,test,transaction_id FROM ad_claims WHERE date(created)>=? AND date(created)<=?',
                (str(start), str(today))):
            if owner not in accounts or test:
                continue
            if status == 'granted':
                ads['skipRights' if (transaction or '').startswith('skip:') else 'verifiedRewards'] += 1
            else:
                ads[status] += 1
        gems = defaultdict(lambda: {'gain': 0, 'spent': 0, 'events': 0})
        for owner, kind, delta in db.execute(
                "SELECT owner,type,delta FROM ledger WHERE currency='gems' AND date(created)>=? AND date(created)<=?",
                (str(start), str(today))):
            if owner in accounts:
                gems[kind]['gain'] += max(0, delta)
                gems[kind]['spent'] += max(0, -delta)
                gems[kind]['events'] += 1
        stages = Counter()
        for a in accounts.values():
            bucket = '60+ / 완료 포함' if a['stage'] >= 60 else f"{((a['stage'] - 1) // 6) * 6 + 1}–{((a['stage'] - 1) // 6 + 1) * 6}"
            stages[bucket] += 1
        return {'generatedAt': now.isoformat(), 'since': str(start), 'timezone': 'UTC',
                'accounts': len(accounts), 'excludedAccounts': ignored,
                'introSeen': sum(a['intro'] for a in accounts.values()),
                'daily': daily, 'cohorts': cohorts, 'purchases': dict(purchases), 'payers': len(payers),
                'ads': dict(ads), 'gems': dict(gems), 'stages': dict(stages)}
    finally:
        db.close()


def table(headers, rows):
    esc = lambda x: html.escape(str(x))
    return '<div class="scroll"><table><thead><tr>' + ''.join('<th>'+esc(h)+'</th>' for h in headers) + '</tr></thead><tbody>' + ''.join('<tr>'+''.join('<td>'+esc(c)+'</td>' for c in row)+'</tr>' for row in rows) + '</tbody></table></div>'


def render(d):
    esc = lambda x: html.escape(str(x))
    p = d['purchases']
    cards = [('서버 계정 · 현재', d['accounts']), ('기간 내 실결제 검증', p.get('verified', 0)),
             ('기간 내 구매 계정', d['payers']), ('해당 구매의 현재 환불', p.get('revoked', 0))]
    body = '<div class="cards">' + ''.join(f'<article>{esc(k)}<strong>{esc(v)}</strong></article>' for k,v in cards) + '</div>'
    body += '<h2>일별 가입 · 서버 활동</h2><p>서버 명령에 성공한 계정입니다. 앱 실행 DAU와 다릅니다. 오늘은 집계 중입니다.</p>'
    body += table(['UTC 날짜','신규 서버 계정','활동 계정','집계'], [(r['date'], r['newAccounts'], r['commandActive'], '진행 중' if r['partial'] else '완료') for r in reversed(d['daily'])])
    body += '<h2>가입 후 D1 / D7 서버 활동 재방문</h2><p>가입일의 정확히 1일 / 7일 뒤 UTC 하루 동안 명령 실행. 해당 하루가 끝나야 계산합니다. 설치 유지율과 다릅니다.</p>'
    def rate(r, k):
        return '관찰 중 / 표본 없음' if r[k] is None else f"{r[k]} / {r['accounts']} ({r[k] / r['accounts']:.0%})"
    body += table(['가입 UTC 날짜','계정','D1','D7'], [(r['date'],r['accounts'],rate(r,'d1'),rate(r,'d7')) for r in reversed(d['cohorts'])])
    body += '<h2>진행 현황 · 현재 스냅샷</h2><p>최고 해금 작전 분포입니다. 구간별 이탈률·승률이 아닙니다.</p>' + table(['해금 작전','계정'], d['stages'].items())
    body += '<h2>보석 원장</h2><p>현금 매출이 아닙니다. 환불 회수는 일반 소비와 구분합니다.</p>' + table(['경로','획득','소비 / 회수','건수'], [(k,v['gain'],v['spent'],v['events']) for k,v in sorted(d['gems'].items())])
    body += '<h2>광고 보상</h2><p>서버 검증 보상이며 광고 노출·수익이 아닙니다. 광고 제거 권리와 테스트 광고는 별도 처리합니다.</p>' + table(['항목','건수'], sorted(d['ads'].items()) or [('기록 없음',0)])
    health = d.get('health')
    body += '<h2>서비스 · 환불 처리</h2>'
    body += '<p>' + ('정상' if health and health.get('ok') else '점검 필요 또는 확인하지 못함') + '</p>'
    if health:
        body += table(['검사','결과'], [(k,'정상' if v.get('ok') else '점검 필요') for k,v in health.get('checks',{}).items()])
        service = health.get('checks', {}).get('services', {}).get('details', {})
        recovery = service.get('refundRecovery', {})
        body += table(['운영 설정','현재 상태'], [
            ('결제 환경',service.get('commerce','확인 불가')),
            ('신규 판매',service.get('newSales','확인 불가')),
            ('광고',service.get('ads','확인 불가')),
            ('환불 실시간 알림 RTDN',service.get('rtdn','확인 불가')),
            ('환불 처리 대기',recovery.get('pending',{}).get('count','확인 불가')),
            ('누락 환불 복구 완료',recovery.get('cursor',{}).get('completed','확인 불가'))])
    return '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>고철 수호대 · 출시 운영</title><style>
body{font:16px system-ui;background:#101b26;color:#edf4f8;max-width:1120px;margin:40px auto;padding:0 24px}p{color:#b2c6d7;line-height:1.7}h2{margin-top:38px}a{color:#8fdfce}.cards{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}article{background:#243847;border-radius:14px;padding:20px}strong{display:block;font-size:32px;margin-top:12px;color:#92e7c9}table{border-collapse:collapse;width:100%}th,td{text-align:left;padding:12px;border-bottom:1px solid #344657;white-space:nowrap}.scroll{overflow:auto}nav{display:flex;gap:18px;flex-wrap:wrap;margin:24px 0}footer{margin:36px 0}@media(max-width:700px){.cards{grid-template-columns:1fr 1fr}}
</style><h1>고철 수호대 · 출시 운영</h1><p>''' + esc(d['generatedAt']) + ' 생성 · ' + esc(d['since']) + '''부터 UTC 집계 · 수동 새로고침 스냅샷</p><nav>''' + ''.join(f'<a href="{PLAY}{path}">{label}</a>' for path,label in [('/statistics','Play 저장된 운영 보고서'),('/reporting/finance/revenue','실제 매출'),('/vitals/metrics/overview','Android 오류·ANR'),('/grow-overview','스토어 유입')]) + '<a href="https://apps.admob.com/">AdMob 광고 수익</a></nav>' + body + f'''<footer><p>테스트·지정 검증 계정 {d['excludedAccounts']}개 제외. 오프라인 플레이·삭제된 계정은 포함하지 않습니다. Play 보고서에는 비공개 테스트 기간이 섞일 수 있습니다. 출시 이후 기간을 선택해 비교하세요.</p><p>테스트 구매 {p.get('testExcluded',0)}건, 검증 유형 미확인 {p.get('unclassified',0)}건은 실결제 집계에서 제외했습니다. 환불은 구매일 기준 집단의 현재 상태이며 환불 발생일 통계가 아닙니다. 매출·수수료·정산금은 Play, 광고 수익은 AdMob에서 확인합니다.</p><p>현재 광고 활성화 여부는 /health/services와 AdMob 승인 상태를 함께 확인하세요. 이 보고서는 광고를 켜거나 결제하지 않습니다.</p></footer></html>'''


def private_write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    fd, temp = tempfile.mkstemp(dir=path.parent)
    try:
        with os.fdopen(fd, 'w') as f:
            f.write(value)
        os.replace(temp, path)
    finally:
        if os.path.exists(temp):
            os.unlink(temp)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--since', default=str(max(date(2026, 11, 23), datetime.now(timezone.utc).date() - timedelta(days=27))))
    parser.add_argument('--db', type=Path, help='Read a local SQLite fixture/snapshot instead of SSH')
    parser.add_argument('--output', type=Path, default=ROOT/'UserSettings/Operations')
    args = parser.parse_args()
    exclusions = {'ids': [], 'hashes': []}
    secrets = ROOT/'UserSettings/Secrets'
    review = secrets/'play-review-account.json'
    if review.exists():
        exclusions['ids'].append(json.loads(review.read_text())['playerId'])
    smoke = secrets/'release040-smoke-account.json'
    if smoke.exists():
        secret = json.loads(smoke.read_text())['secret']
        exclusions['hashes'].append(hashlib.sha256(secret.encode()).hexdigest().upper())
    custom = secrets/'operations-exclusions.json'
    if custom.exists():
        exclusions['ids'].extend(json.loads(custom.read_text())['ids'])
    if args.db:
        data = aggregate(args.db, args.since, exclusions)
    else:
        remote_path = '/var/lib/docker/volumes/scrapguard-beta_game_data/_data/scrapguard.db'
        source = Path(__file__).read_text().rsplit("\nif __name__ == '__main__':", 1)[0]
        # Account identifiers/hash exclusions stay in the SSH stdin stream, never argv/logs.
        source += '\nprint(json.dumps(aggregate(' + repr(remote_path) + ',' + repr(args.since) + ',' + repr(exclusions) + ')))\n'
        command = ['ssh', '-i', str(secrets/'LightsailDefaultKey-ap-northeast-2.pem'),
                   '-o', 'UserKnownHostsFile='+str(secrets/'lightsail_known_hosts'), '-o', 'StrictHostKeyChecking=yes',
                   '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=10', 'ubuntu@52.78.162.152', 'sudo -n python3 -']
        result = subprocess.run(command, input=source, text=True, capture_output=True, timeout=60)
        if result.returncode:
            raise RuntimeError('SSH collection failed; existing dashboard was not replaced. ' + result.stderr[-500:])
        data = json.loads(result.stdout)
        health = subprocess.run(['python3', str(ROOT/'Tools/service_health.py'), '--url', 'https://api.scrapguard.win'],
                                capture_output=True, text=True, timeout=40)
        data['health'] = json.loads(health.stdout) if health.stdout else {'ok': False}
    private_write(args.output/'launch.json', json.dumps(data, ensure_ascii=False, indent=2))
    private_write(args.output/'index.html', render(data))
    print(args.output/'index.html')


if __name__ == '__main__':
    main()
