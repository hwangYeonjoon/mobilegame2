#!/usr/bin/env python3
"""Offline configuration checks. Never prints credential values or contacts providers."""
import argparse
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]

def inspect(values, client, signing=False):
    checks = []
    def add(name, ok, needed):
        checks.append({'check': name, 'status': 'ready' if ok else 'missing', 'needed': '' if ok else needed})
    def https(value):
        from urllib.parse import urlparse
        u = urlparse(value or '')
        return u.scheme == 'https' and bool(u.hostname) and not u.username and not u.password and not u.hostname.endswith('example.com')
    add('server_https', https(client.get('releaseEndpoint')), '운영 HTTPS 주소를 ClientConnection.json에 설정')
    add('google_login', bool(values.get('GOOGLE_WEB_CLIENT_ID', '').endswith('.apps.googleusercontent.com')), 'Google OAuth Web 클라이언트 ID')
    credentials = values.get('GOOGLE_APPLICATION_CREDENTIALS', '')
    add('google_server_credentials', bool(credentials and Path(credentials).is_file()), '검사 실행 환경에서 읽을 수 있는 서비스 계정 파일 경로')
    add('play_package', values.get('PLAY_PACKAGE_NAME') == client.get('packageName') and bool(client.get('packageName')), '서버와 앱의 동일한 Play 패키지명')
    add('commerce_mode', values.get('COMMERCE_MODE') in ('license_test','production'), '먼저 license_test 모드로 실제 거래 검증')
    add('production_gate', values.get('COMMERCE_MODE') != 'production' or values.get('COMMERCE_RELEASE_READY') == 'true', '실제 거래 검증 후 production 준비 완료 설정')
    app = client.get('adMobAppId', '')
    unit = client.get('rewardedUnitId', '')
    add('admob_game_app', bool(re.fullmatch(r'ca-app-pub-\d{16}~\d{10}', app)) and '3940256099942544' not in app, '같은 AdMob 계정에 이 게임을 별도 앱으로 등록')
    add('admob_game_unit', bool(re.fullmatch(r'ca-app-pub-\d{16}/\d{10}', unit)) and '3940256099942544' not in unit and unit == values.get('ADMOB_REWARDED_UNIT_ID'), '이 게임 보상형 광고 단위: 앱·서버 값 일치')
    add('admob_sdk_enabled', client.get('adsEnabled') is True, 'SDK는 연결 검증 준비 후 활성화')
    add('admob_reward_contract', values.get('ADMOB_REWARD_ITEM') == 'reward' and values.get('ADMOB_REWARD_AMOUNT') == '1', 'AdMob 콘솔 보상은 reward / 1, 실제 게임 지급량은 서버 결정')
    for key in ('privacyUrl','supportUrl','deletionUrl'):
        add(key, https(client.get(key)), '실제 공개 HTTPS 안내 페이지')
    if signing:
        add('upload_signing', bool(values.get('SCRAP_KEYSTORE_PATH') and Path(values['SCRAP_KEYSTORE_PATH']).is_file() and all(values.get(k) for k in ('SCRAP_KEY_ALIAS','SCRAP_KEYSTORE_PASSWORD','SCRAP_KEY_PASSWORD'))), '로컬 업로드 키와 서명 환경 변수')
    return {'configuration_ready': all(c['status']=='ready' for c in checks), 'checks': checks,
            'not_verified': ['Google 상품·OAuth·AdMob 콘솔 등록 상태', '실제 구매·환불·광고 SSV', '스토어 심사·실기기 동작', '공개 서버 연결·부하·백업 복구'],
            'note': '설정 점검이며 출시 승인이나 외부 연결 검증 결과가 아닙니다.'}

def main():
    import os
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--env-file', type=Path)
    p.add_argument('--signing', action='store_true')
    p.add_argument('--output', type=Path, default=ROOT/'TestResults/release-preflight.json')
    args = p.parse_args()
    values = {}
    if args.env_file:
        for line in args.env_file.read_text().splitlines():
            if line.strip() and not line.lstrip().startswith('#') and '=' in line:
                key, value = line.split('=', 1); values[key.strip()] = value.strip().strip('"').strip("'")
    values.update(os.environ)
    client = json.loads((ROOT/'Assets/Resources/ServiceSettings.json').read_text())
    client.update(json.loads((ROOT/'Assets/Resources/ClientConnection.json').read_text()))
    # Package is a source constant; avoid relying on stale editor PlayerSettings.
    source=(ROOT/'Assets/Game/Core/AppRelease.cs').read_text()
    match=re.search(r'Package\s*=\s*"([^"]+)"',source)
    client['packageName']=match.group(1) if match else ''
    report=inspect(values,client,args.signing)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(report,ensure_ascii=False,indent=2))
    return 0 if report['configuration_ready'] else 2
if __name__ == '__main__':
    raise SystemExit(main())
