#!/usr/bin/env python3
"""Render the local --ops metrics aggregate; no server credentials or CDN needed."""
import argparse
import json
from pathlib import Path


def render(data):
    payload = json.dumps(data, ensure_ascii=False).replace('<', '\\u003c').replace('>', '\\u003e').replace('&', '\\u0026')
    return TEMPLATE.replace('__DATA__', payload)


TEMPLATE = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>고철 수호대 · 운영 지표</title><style>
:root{color-scheme:dark;font-family:system-ui,sans-serif;color:#e7eff5;background:#101a24}body{max-width:1200px;margin:40px auto;padding:0 24px}h1{font-size:30px;margin-bottom:8px}h2{font-size:21px;margin-top:32px}p,.muted{color:#aabccd;line-height:1.6}select{padding:10px 20px;border:1px solid #526779;border-radius:9px;background:#203342;color:inherit;font-size:16px}.cards{display:grid;grid-template-columns:repeat(4,1fr);gap:14px;margin:25px 0}.card{background:#20303e;padding:20px;border-radius:12px}.value{font-size:30px;color:#72e6be;margin-top:10px}.scroll{overflow:auto}table{width:100%;border-collapse:collapse;white-space:nowrap}th,td{text-align:left;padding:13px 10px;border-bottom:1px solid #2d4050}th{color:#b7c9d7;font-size:13px}td{font-size:14px}.bar{display:inline-block;background:#72e6be;height:7px;border-radius:4px;margin-right:8px;max-width:80px}.warn{color:#ffca79}footer{margin:35px 0;font-size:13px}@media(max-width:700px){body{margin-top:24px;padding:0 16px}.cards{grid-template-columns:1fr 1fr}.card{padding:14px}.value{font-size:24px}}
</style><header><p>SCRAP GUARD · LOCAL OPERATIONS</p><h1>고철 수호대 운영 지표</h1><p id="stamp"></p><label>집계 기간 <select id="period"><option value="7">최근 7일</option><option value="30" selected>최근 30일</option><option value="90">최근 90일</option></select></label></header>
<div id="cards" class="cards"></div><h2>의상 체험 → 구매 · 현재 장착</h2><p>전환율은 통계에 동의한 미보유 체험 계정 기준, 체험 후 7일 내 구매입니다. 최근 체험은 관찰 중입니다.<br>장착률은 현재 보유 계정 대비 해당 의상 장착 계정 비율입니다. 기간별 구매 건수와 분모가 다릅니다.</p><div class="scroll"><table><thead><tr><th>의상</th><th>체험 계정</th><th>전환 계정</th><th>전환율</th><th>구매 건수</th><th>현재 보유</th><th>현재 장착</th><th>장착률</th></tr></thead><tbody id="outfits"></tbody></table></div>
<h2>전시 배경 · 현재 사용</h2><p>유료 배경과 플레이 기념관의 현재 보유/적용 현황입니다. 기간별 매출이나 체험 전환율이 아닙니다.</p><div class="scroll"><table><thead><tr><th>배경</th><th>획득 방식</th><th>현재 보유</th><th>현재 적용</th><th>적용률</th></tr></thead><tbody id="themes"></tbody></table></div>
<h2>보석 획득 · 소비 원장</h2><p>무료·유료 보석을 포함한 지갑 증감입니다. 원화 매출이 아니며 환불 회수는 아래에서 별도로 표시합니다.</p><div class="scroll"><table><thead><tr><th>발생 경로</th><th>획득</th><th>소비 / 회수</th><th>발생 건수</th></tr></thead><tbody id="flows"></tbody></table></div>
<h2>콘텐츠 공개 일정</h2><p>한국 시간 기준 · 이미 보유한 의상은 판매 종료 후에도 사용 가능합니다.</p><div class="scroll"><table><thead><tr><th>의상</th><th>공개</th><th>종료</th><th>상태</th><th>최소 빌드</th></tr></thead><tbody id="content"></tbody></table></div>
<h2>환불 복구 상태</h2><p id="refunds"></p><footer id="notes"></footer><script id="data" type="application/json">__DATA__</script><script>
const d=JSON.parse(document.getElementById('data').textContent),$=id=>document.getElementById(id),n=v=>Number(v||0).toLocaleString('ko-KR'),date=v=>v?new Date(v).toLocaleString('ko-KR',{timeZone:'Asia/Seoul'}):'없음';
function cell(row,value){let t=document.createElement('td');t.textContent=value;row.appendChild(t);return t}
function row(id,values){let r=document.createElement('tr');values.forEach(v=>cell(r,v));$(id).appendChild(r);return r}
function rate(a,b){return b?((a/b)*100).toFixed(1)+'%':'—'}
$('stamp').textContent='보고서 생성 '+date(d.generatedAt)+' · 이벤트 수집 시작 '+date(d.captureStarted)+' · '+(d.includeTests?'테스트 계정 포함':'테스트 계정 제외');
function draw(){const w=d.windows.find(x=>x.days===Number($('period').value));for(let id of ['cards','outfits','themes','flows','content'])$(id).replaceChildren();let gain=0,spent=0,refund=0;for(let f of w.gemFlows){gain+=Number(f.gained);if(f.type==='play_refund')refund+=Number(f.spent);else spent+=Number(f.spent)}
for(let [label,value] of [['활동 계정',w.activeAccounts],['보석 획득',gain],['보석 소비',spent],['환불 보석 회수',refund]]){let card=document.createElement('div');card.className='card';card.textContent=label;let v=document.createElement('div');v.className='value';v.textContent=n(value);card.appendChild(v);$('cards').appendChild(card)}
for(let o of w.outfits)row('outfits',[o.name,n(o.previews),n(o.converted),rate(o.converted,o.previews),n(o.purchases),n(o.owners),n(o.equipped),rate(o.equipped,o.owners)]);
for(let t of d.themes||[])row('themes',[t.name,t.earned?'작전 완료':'보석 구매',n(t.owners),n(t.equipped),rate(t.equipped,t.owners)]);
for(let f of w.gemFlows)row('flows',[f.type==='play_refund'?'play_refund · 환불 회수':f.type,n(f.gained),n(f.spent),n(f.events)]);if(!w.gemFlows.length)row('flows',['기간 내 보석 원장 없음','—','—','—']);
for(let c of d.content.outfits){let now=new Date(d.content.serverTime),status=c.paused?'판매 중지':c.startsAt&&new Date(c.startsAt)>now?'공개 예정':c.endsAt&&new Date(c.endsAt)<=now?'판매 종료':'판매 중';row('content',[w.outfits.find(o=>o.id===c.id)?.name||c.id,date(c.startsAt),date(c.endsAt),status,c.minimumBuild])}
let r=d.refunds;$('refunds').textContent='목록 복구 완료 '+date(r.cursor.completed)+' · 대기 '+n(r.pending.count)+'건 · 최대 재시도 '+n(r.pending.attempts)+'회 · 재검증 토큰 없음 '+n(r.coverage.missingTokens)+'건 · 재검증 실패 '+n(r.coverage.failures)+'건'+(r.cursor.gap?' · 30일 초과 공백 감지: 개별 구매 재검증과 대조 필요':'');$('refunds').className=r.cursor.gap||r.coverage.failures||r.coverage.missingTokens||r.pending.attempts?'warn':'';
}for(let note of d.notes){let p=document.createElement('p');p.textContent=note;$('notes').appendChild(p)}$('period').onchange=draw;draw();
</script></html>'''

if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--input', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    data = json.loads(args.input.read_text())
    args.output.write_text(render(data))
    print(args.output)
