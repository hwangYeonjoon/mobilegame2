# 이선생 게임공방 — 채널 디자인

2026-11-25 YouTube 채널 꾸미기 요청으로 제작하고 공개 채널에서 반영을 확인했다.

- 채널: https://www.youtube.com/@mrGrooveSpher
- 채널 ID: `UCNFKX_NnUQPGwRyEz0ADE9g`
- `avatar-v1.png`: 게임패드와 렌치 심벌. 원형 프로필용.
- `banner-v1.png`: 2560×1440 PNG. 로봇과 배송 트럭을 담은 공방 배너.
- 제작: 내장 imagegen 도구. API/CLI 생성은 사용하지 않았다. 배너 생성 결과 1672×941을 업로드 규격 2560×1440으로 리샘플링했다.
- YouTube 배너 자르기 화면에서 제목·부제·심벌이 모든 기기 표시 영역 안에 있는 것을 확인했다. 실제 휴대폰 앱 검증은 하지 않았다.
- 프로필·배너·고철 수호대 Google Play 링크 게시 후 공개 채널 새로고침으로 표시를 확인했다.
- 홈: 비구독자 트레일러와 재방문 구독자 추천 영상에 `5yfN0J4dO9Q`를 지정했다. 동영상 목록 섹션도 추가했다. Studio 새로고침 후 두 대표 영상 설정과 동영상 1개가 유지됐다. 공개 홈에서 실제 영상 재생을 확인했다.
- 이름·기존 핸들을 유지했다. 앱 소스, 앱 아이콘, Play 심사 요청은 이 작업에서 변경하지 않았다.
- 참고: 공개 홈에서 영상 자막 `어떤 로봇을 만들어 볼까요?`가 표시됐고, 설명의 Google Play URL도 링크로 노출됐다. 이는 해당 화면의 확인 결과이며 전체 계정 고급 기능 인증 여부를 별도로 확인한 것은 아니다.

## 생성 프롬프트

### 프로필

Use case: logo-brand. Asset type: square YouTube channel avatar for the Korean indie game developer channel '이선생 게임공방'. Create a polished, original, friendly game-workshop emblem that remains immediately recognizable in a tiny circular avatar. A single chunky cream-colored handheld game controller combined naturally with an amber wrench, as one compact emblem, on a deep midnight teal background. Subtle small four-point warm spark above it, simple tactile painted/enamel finish, crisp dark outlines, sophisticated friendly indie studio identity, restrained 3 colors cream / warm orange amber / deep teal. Centered symmetrical composition, generous 18% safe margin for circular crop, all important details inside central 65% of square. No letters, no text, no extra symbols, no face, no person, no watermark, no third-party logos. Clean uncluttered professional avatar, 1024x1024.

### 배너

참조: 위에서 생성한 프로필 이미지.

Use case: ads-marketing. Create a finished YouTube channel banner for the Korean indie game developer brand '이선생 게임공방'. Reference image is the approved channel avatar: preserve its midnight teal, cream and amber palette and painted/enamel illustrative finish, but create an entirely new wide banner layout. Deliver a large 16:9 raster canvas 2560x1440 pixels (at least 2048x1152), NOT a mockup. Critical YouTube crop requirement: all text and emblem must fit inside the central horizontal safe strip, x=560..2000 and y=555..885 on a 2560x1440 canvas. Very large quiet margins above and below; those regions can be cropped on mobile. Center safe strip contains exact Korean headline '이선생 게임공방' in bold beautifully legible cream Korean display lettering, with smaller exact subtitle '게임 개발 · 업데이트 · 실제 플레이'. Keep text in the center, not near any edges, no additional writing. A small version of the controller-and-wrench emblem from reference sits immediately left of the headline, inside safe strip. Scene: an inviting indie game workshop illustrated in warm gouache/enamel style with a deep teal backdrop and amber work lights. At far left outer margin a small toy-like friendly orange robot and mechanical parts; at far right outer margin a miniature mint delivery truck, cardboard parcels and a little potted flower, hinting at both action games and cozy delivery puzzle games. Side props are secondary decorative and may be cropped; do not let them interfere with center title. Premium restrained composition, strong hierarchy, clean recognizable silhouette, subtle painted grain, no human face, no clutter, no watermarks, no third-party branding, no UI frames. The only text is the exact Korean headline and subtitle above.
