#!/usr/bin/env python3
"""Reproducible real-footage Shorts edits and public campaign metadata. No upload."""
import hashlib
import html
import json
import subprocess
from pathlib import Path
from urllib.parse import urlencode

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Store/Promotions/acquisition-20261126'
WORK = ROOT / 'TestResults/Acquisition20261126'
RAW = ROOT / 'TestResults/Promotion041'
CODEX = ROOT / 'TestResults/Navigation041FinalAndroid'
PACKAGE = 'io.github.hwangyeonjoon.scrapguard'
CAMPAIGNS = ['yt_gameplay_20261126', 'yt_channel_20261126',
             'yt_short_transform_20261126', 'yt_short_design_20261126',
             'yt_short_assemble_20261126']


def link(campaign):
    tags = dict(utm_source='youtube', utm_medium='organic_video', utm_campaign=campaign)
    return 'https://play.google.com/store/apps/details?' + urlencode(
        dict(id=PACKAGE, **tags, referrer=urlencode(tags)))


def run(args):
    subprocess.run(args, check=True)


def caption(name, headline, subline):
    # Native SVG typography overlays; gameplay pixels are never generated.
    svg = WORK / (name + '.svg')
    svg.write_text(f'''<svg xmlns="http://www.w3.org/2000/svg" width="720" height="1280">
      <rect width="720" height="128" fill="#0a1822"/>
      <text x="36" y="49" font-family="Apple SD Gothic Neo" font-weight="bold" font-size="22" fill="#63efc8">고철 수호대 · 실제 게임 화면</text>
      <text x="36" y="98" font-family="Apple SD Gothic Neo" font-weight="bold" font-size="39" fill="white">{html.escape(headline)}</text>
      <rect y="1058" width="720" height="222" fill="#0a1822"/>
      <text x="36" y="1100" font-family="Apple SD Gothic Neo" font-weight="bold" font-size="28" fill="#ffdb7e">{html.escape(subline)}</text>
      <text x="36" y="1145" font-family="Apple SD Gothic Neo" font-size="24" fill="white">설치 링크는 채널 프로필에</text>
      </svg>''')
    png = WORK / (name + '.png')
    run(['rsvg-convert', str(svg), '-o', str(png)])
    return png


SPECS = [
    dict(slug='transform', title='이 작은 대원이 로봇에 탄다면? 🤖 | 고철 수호대 #Shorts',
         note='작은 대원 → 부품으로 제작 → 포격 로봇 탑승. 실제 0.41 Android 플레이를 컷 편집했습니다.',
         cuts=[('assembly.mp4',5,1,'이 로봇, 직접 만듭니다','부수고 · 회수하고 · 탑승하라'),
               ('battle.mp4',146,2,'작은 대원으로 시작해서','부품을 모으면'),
               ('assembly.mp4',0,3,'직접 조립하고','포신 4개로 포격형 제작'),
               ('assembly.mp4',3,8,'이 로봇에 탑승합니다','부수고 · 회수하고 · 탑승하라')]),
    dict(slug='design', title='포격 전차 vs 돌파 보행기, 어떤 로봇을 만들까요? | 고철 수호대 #Shorts',
         note='게임 내 도감 화면으로 포격 전차와 돌파 보행기의 모습·필요 부품을 비교합니다. 후반 전투 장면은 포격형의 실제 플레이입니다. 돌파형 전투 비교 영상은 아닙니다.',
         cuts=[('06-robot-1-1.png',0,4,'포신을 모으면 포격 전차','로봇 도감 · 기체와 부품 확인'),
               ('06-robot-5-1.png',0,4,'구동부를 모으면 돌파 보행기','어떤 로봇을 만들고 싶나요?'),
               ('assembly.mp4',4,6,'고른 로봇에 직접 탑승','포격 전차 실제 플레이')]),
    dict(slug='assemble', title='모은 부품이 전투 중 로봇이 되는 순간 | 고철 수호대 #Shorts',
         note='도감에서 필요한 적을 확인하고, 전투에서 부품을 모아 제작·탑승하는 흐름입니다. 서로 다른 시점의 실제 플레이를 이어 붙였으며 위기 탈출이나 승리를 증명하는 영상은 아닙니다.',
         cuts=[('navigation.mp4',32,4,'어떤 적을 잡아야 할까?','도감에서 부품 획득처 확인'),
               ('battle.mp4',146,3,'전투에서 부품을 회수하고','전투 부품은 매 판 다시 수집'),
               ('assembly.mp4',0,3,'준비됐다면 제작하고 탑승','조립 버튼 한 번으로'),
               ('assembly.mp4',5,6,'모은 부품이 내 로봇으로','고철 수호대 · Google Play 출시')]),
]


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    links = {c: link(c) for c in CAMPAIGNS}
    (OUT/'campaign-links.json').write_text(json.dumps(links, ensure_ascii=False, indent=2)+'\n')
    metadata = []
    for spec in SPECS:
        segments, evidence = [], []
        for i,(source,start,duration,headline,subline) in enumerate(spec['cuts']):
            still = source.endswith('.png')
            path = (CODEX if still else RAW)/source
            overlay = caption(f"{spec['slug']}-{i}", headline, subline)
            segment = WORK / f"{spec['slug']}-{i}.mp4"
            cmd=['ffmpeg','-hide_banner','-loglevel','error','-y']
            cmd += ['-loop','1','-i',str(path)] if still else ['-ss',str(start),'-i',str(path)]
            cmd += ['-loop','1','-i',str(overlay),'-filter_complex',
                    '[0:v]crop=540:832:0:128,scale=592:912,setsar=1,pad=720:1280:24:140:color=0x0a1822[game];[game][1:v]overlay=0:0[v]',
                    '-map','[v]','-an','-t',str(duration),'-r','30','-c:v','libx264','-preset','fast','-crf','19','-threads','2','-pix_fmt','yuv420p',str(segment)]
            run(cmd);segments.append(segment)
            evidence.append(dict(source=str(path.relative_to(ROOT)),start=start,duration=duration,
                                 kind='static codex QA capture' if still else 'real Android gameplay',
                                 sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
        concat=WORK/(spec['slug']+'.concat')
        concat.write_text(''.join(f"file '{p}'\n" for p in segments))
        duration=sum(c[2] for c in spec['cuts'])
        video=OUT/f"short-{spec['slug']}.mp4"
        run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(concat),
             '-stream_loop','-1','-i',str(ROOT/'Assets/Resources/Audio/Licensed/Overdrive.ogg'),
             '-map','0:v','-map','1:a','-c:v','copy','-af',f'volume=0.25,afade=t=out:st={duration-1}:d=1',
             '-c:a','aac','-b:a','128k','-t',str(duration),'-movflags','+faststart',str(video)])
        campaign=f"yt_short_{spec['slug']}_20261126"
        desc=(f"고철 수호대 — Google Play에서 플레이하세요.\n설치: 채널 프로필의 ‘고철 수호대’ 링크\n\n"
              f"{spec['note']}\n\n직접 열기·복사용 링크:\n{links[campaign]}\n\n"
              '영상은 Android 에뮬레이터에서 녹화한 게임 화면이며 실기기 성능을 뜻하지 않습니다. 도감 비교는 정지 화면입니다.\n'
              '음악: Overdrive — Alexander Ehlers (tricksntraps), CC0 1.0. 기기 녹음 대신 음악을 편집했습니다.\n'
              '#고철수호대 #로봇게임 #인디게임 #Shorts')
        metadata.append(dict(slug=spec['slug'],title=spec['title'],description=desc,file=video.name,
                             duration=duration,sources=evidence,sha256=hashlib.sha256(video.read_bytes()).hexdigest()))
    (OUT/'shorts.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+'\n')
    description=('고철 수호대 — Google Play 출시! 적을 부숴 모은 부품으로 로봇을 조립하고 직접 탑승하세요.\n'
                 f"▶ Android 설치\n{links[CAMPAIGNS[0]]}\n\n"
                 '로봇 도감에서 만들고 싶은 기체와 필요한 부품을 확인하고, 60개 작전에 도전해 보세요. 전투 부품은 매 판 다시 회수합니다.\n\n'
                 '영상은 0.41 버전의 실제 Android 에뮬레이터 플레이를 편집했습니다. 현재 Google Play에서는 후속 출시 버전을 제공합니다.\n'
                 '썸네일은 실제 게임 로봇 디자인을 사용한 홍보 이미지입니다.\n'
                 '이선생 게임공방에서 직접 개발한 게임의 제작 과정과 업데이트를 소개합니다.\n'
                 '음악: Overdrive — Alexander Ehlers (tricksntraps), CC0 1.0.\n\n#고철수호대 #인디게임 #로봇게임')
    (OUT/'existing-video.json').write_text(json.dumps(dict(id='5yfN0J4dO9Q',title='적을 부숴 모은 부품으로 로봇을 만든다면? | 고철 수호대',description=description),ensure_ascii=False,indent=2)+'\n')
    print(json.dumps([dict(file=m['file'],seconds=m['duration']) for m in metadata],ensure_ascii=False))


if __name__ == '__main__':
    main()
