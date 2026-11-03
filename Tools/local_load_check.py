#!/usr/bin/env python3
"""Bounded concurrent read check against the isolated loopback Docker stack only."""
import concurrent.futures
import json
from pathlib import Path
import secrets
import time
from urllib.request import Request,urlopen
BASE='http://127.0.0.1:5190'
secret=secrets.token_urlsafe(32)
headers={'Content-Type':'application/json','X-Scrap-Build':'16','X-Scrap-Rules':'11','Authorization':'Bearer '+secret}
def request(path,data=None):
    with urlopen(Request(BASE+path,data=None if data is None else json.dumps(data).encode(),headers=headers),timeout=10) as response:
        return json.load(response)
initial=request('/v1/auth/guest',{'secret':secret,'inviteCode':'local-integration-test-invite-03'})
def sample(_):
    start=time.monotonic();state=request('/v1/bootstrap')
    assert state['playerId']==initial['playerId'] and state['revision']==initial['revision']
    assert state['state']['gems']==initial['state']['gems']
    return (time.monotonic()-start)*1000
started=time.monotonic()
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    latencies=sorted(pool.map(sample,range(40)))
report={'requests':40,'concurrency':4,'errors':0,'seconds':time.monotonic()-started,'p95_ms':latencies[37],
        'scope':'loopback authenticated reads only; not public capacity, combat settlement, provider calls or prolonged load'}
path=Path(__file__).resolve().parents[1]/'TestResults/local-load-016.json';path.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
