# mobilegame2 안드로이드 생존 성장 게임

탕탕특공대의 한 손 생존 전투를 참고해, 짧은 전투와 장비·캐릭터 육성, 출석, NPC 상점, 보석 결제, 광고 보상으로 이어지는 Android 게임을 만든다.

**0.6.0 개조·수집 확장 프로토타입**: 5지역 30작전에 개조 조합 3종, 전투 분석, 지역 설계도와 확정 도색 제작, 적·보스 도감, 길드 지원 등장 카드와 주간 기록 카드 공유를 추가했다. 6역할 적과 5지역 보스는 전용 이미지와 기본·공격 준비·공격·피격 상태를 사용한다. 자세한 규칙과 한계는 [0.6 구현 문서](docs/11-workshop-and-collection.md), 지역 구성은 [0.5 구현 문서](docs/09-campaign-expansion.md)에 있다. 소규모 공개 베타용 Docker/HTTPS 배포 템플릿과 초대 코드·백업 기능도 준비했다.

출격 화면에서 **특수 던전 · 코어 균열**과 **길드 · 지원 대원**으로 이동할 수 있다. 작전 05 완료 이후 90초 정식 도전과 전용 코어 파편 교환, 주간 기록판, 트로피 전시대를 이용한다. 길드에서는 동료의 대원을 하루 3회 빌려 일반 구역에서 호출할 수 있다. 자세한 규칙은 [0.4 구현 문서](docs/08-rift-guild-and-weekly-records.md)에 있다.

로컬 체험은 서버 없이 플레이할 수 있다. 별도로 실행한 서버에 연결하면 새 게스트 계정으로 시작하며, 출석·재화·장비·성장·전투 보상을 서버가 저장하고 확정한다. 로컬 재화는 서버에 가져오지 않는다.

실제 Google Play 결제·AdMob 광고·Google 로그인은 아직 연결하지 않았다. 상점의 현금 상품은 준비 중으로 표시하고, 로컬 체험의 설정 → 테스트 도구에서 체험 재화와 광고 스킵을 제공한다. 서버 계정에서는 테스트 지급을 차단한다. 현재 빌드는 스토어 배포본이 아니며 개발 빌드만 허용한다.

## 실행

- Unity `6000.3.7f1`로 이 폴더를 열고 `Assets/Scenes/Workshop.unity`에서 Play를 누른다. 씬의 카메라 외 UI와 전투 오브젝트는 실행 시 생성된다.
- Android APK: `Builds/Android/ScrapGuard-prototype.apk` — 0.6.0 개발 빌드 생성·서명·16KB 정렬 확인 완료. 실기기 실행은 미검증.
- macOS 미리보기: `Builds/macOS/ScrapGuard.app`.
- 조작은 화면 드래그 또는 WASD·방향키. 공격은 자동이며 레벨업 때 스킬을 선택한다. 필살기는 충전 후 우측 하단 버튼 또는 Space. 빌린 길드 대원은 전투 시작 8초 후 지원 버튼 또는 Q로 호출한다. 일시정지는 우측 상단 또는 ESC.
- 빠른 검증은 설정 → 테스트 도구 → 45초 빠른 전투 테스트를 사용한다. 보스 처치에 걸리는 시간에 따라 종료 시각은 늘어난다.

## 서버 연결과 전시실

```sh
dotnet run --project Server/ScrapGuard.Server/ScrapGuard.Server.csproj
```

.NET 10 SDK가 필요하다. 기본 주소는 `http://127.0.0.1:5187`이며 DB는 서버 프로젝트의 `data/scrapguard.db`에 생긴다. 실행한 서버를 유지한 상태에서 게임의 **설정 → 계정·서버 연결**에 주소를 입력한다. 복구 코드를 비워 두면 새 계정을 만들고, 기존 개인 복구 코드를 입력하면 같은 서버의 계정으로 복구한다.

Android USB 테스트는 `adb reverse tcp:5187 tcp:5187` 실행 후 같은 루프백 주소를 입력한다. 다른 네트워크의 친구와 플레이하려면 별도의 HTTPS 서버 배포가 필요하다. 앱에 공개 운영 서버 주소가 포함되어 있지 않다.

**전시실 → 내 프로필 꾸미기**에서 닉네임·대표 대원·장비·칭호·외형을 고른다. 카드 공유는 Android 공유 창을 열고, macOS에서는 임시 캐시의 `showcase/`에 PNG를 저장한다. 공개 ID와 개인 복구 코드는 다른 값이다. 개인 복구 코드를 공유하면 계정에 접근할 수 있으므로 개인적으로 보관한다.

서버 전시 광장은 공개 동의한 같은 서버의 계정만 표시한다. 응원·신고·차단·차단 해제와 앱 내 계정 삭제를 제공한다. 랭킹이나 응원 수에 따른 재화 보상은 없다. 서버 실행의 기본 구조는 [0.2 구현 문서](docs/05-showcase-and-server.md), 최신 아트·필살기·베타 배포와 백업 절차는 [0.3 구현 문서](docs/06-art-combat-and-operations.md)에 정리했다.

## 구현 범위

| 영역 | 구현 |
| --- | --- |
| 전투 | 이동, 자동 조준·사격, 6종 장비별 사격, 톱날·코일·드론, 경험치·스킬 선택, 개조 조합 3종, 중간·최종 보스, 대원별 필살기, 피해 원인·조합 분석 |
| 진행 | 5지역 30작전, 생존/보스 목표·전체 지도·최초 보상·해금; 철로 포격·오염·빙판·컨베이어·이동 충돌 |
| 성장 | 캐릭터 3종, 장비 정의 12종·3등급, 강화·이전·장착·잠금·판매 |
| 경제 | 7일 누적 출석, 일일 임무, 골드·부품·보석, 확정 상점, 구매·광고 체험 일일 한도 |
| 보상 | 결과 정산·재도전·최초 클리어, 지역 설계도·확정 도색 제작·적과 보스 도감, 광고 체험과 스킵 체험, 중복 지급 방지 |
| 저장 | 거래 실패 시 메모리 롤백, 파일 교체 저장, 백업 복구, 손상 파일 보존 |
| 화면 | 한국어 UI, 안전 영역 대응, 스크롤 목록, 구매·판매 확인, 설정과 효과음 |
| 전시 | 대표 대원·장비 3종·칭호 6종·프레임 5종·도색 8종(기존 3+지역 제작 5), 프로필·주간 최고 기록 카드 PNG, Android 공유 창 |
| 소셜 | 공개 동의, ID 검색, 광장 페이지, 응원, 신고 접수, 양방향 차단·해제, 20인 길드·지원 등록/대여 |
| 특수 던전 | 코어 균열 90초, 주간 기록·달성 보상·전용 파편 교환·트로피 전시 |
| 서버 | ASP.NET Core + SQLite, 게스트 복구, 서버 시각, 거래 원장, 요청 재시도, 입력 재현 전투 정산 |

로컬 파일과 시간은 사용자가 변경할 수 있다. 서버 계정은 별도의 권한·원장을 사용하지만 실제 유료 결제의 검증·환불·권리 처리는 미구현이다. 현재 서버도 공개 운영 준비를 마친 배포본은 아니다.

## 빌드와 검증

```sh
dotnet run --project Tools/CoreChecks/CoreChecks.csproj
dotnet run --project Server/Checks/Checks.csproj
unity test . --mode EditMode --output TestResults/editmode.xml -- -nographics
unity run . -- -nographics -executeMethod ScrapGuard.Editor.ProjectBuilder.BuildMac
unity run . -- -nographics -buildTarget Android -executeMethod ScrapGuard.Editor.ProjectBuilder.BuildAndroid
```

`unity` 명령이 PATH에 없으면 `/Users/kd-server/.unity/bin/unity`를 사용한다. Android 빌드는 Android Build Support, SDK 36, NDK, OpenJDK가 필요하다. API 26 이상·ARM64 기기를 대상으로 한다. Android SDK 36은 target SDK이며 최소 설치 OS와 다르다.

Unity 메뉴에서도 **Scrap Guard → Build Android APK / Build macOS Preview**를 실행할 수 있다. `Configure Project`는 초기 씬과 Android 설정을 준비한다. 실수로 운영 빌드를 만들지 않도록 `PrototypeBuildGuard`가 Development가 아닌 빌드를 차단한다.

저장 위치는 `Application.persistentDataPath/scrap-guard-v1.json`이며 `.bak`에 직전 저장이 남는다. macOS에서는 일반적으로 `~/Library/Application Support/ScrapGuard/고철 수호대/`다. 테스트는 별도 임시 경로를 사용한다.

## 기본 방향

| 항목 | 제안 |
| --- | --- |
| 플랫폼 | Android 우선, Google Play 배포 |
| 조작 | 세로 화면, 한 손 이동, 자동 공격 |
| 플레이 | 5~8분 생존 전투와 영구 성장 |
| 가제와 소재 | 고철 수호대 — 드론과 재활용 무기로 폭주 로봇을 정리하는 정비사 |
| 엔진 | Unity 6.3 LTS 계열 후보, C# 2D |
| 상점 | 시스템 상점에서 확정 구매, 불필요한 장비를 골드로 판매 |
| 핵심 BM | 보석 충전, 확정 캐릭터 해금, 영구 광고 스킵 |
| 첫 출시 범위 | 캐릭터 3종, 스테이지 30개, 무기 6종, 출석·상점·결제·광고 |

## 설계 문서

1. [게임과 성장 설계](docs/01-game-design.md) — 전투, 차별점, 캐릭터, 장비, 화면과 초기 콘텐츠.
2. [재화와 수익모델 설계](docs/02-economy-and-bm.md) — 출석, 아이템 구매·판매, 재화 흐름, 상품, 광고 규칙.
3. [서버와 안드로이드 출시 계획](docs/03-architecture-and-release.md) — 데이터, 결제 검증, 개발 순서, 테스트와 출시 기준.
4. [프로토타입 구현과 검증](docs/04-prototype-verification.md) — 현재 산출물, 테스트 결과와 운영 버전에 남은 작업.
5. [전시실과 서버 계정](docs/05-showcase-and-server.md) — 외형 BM, 전시·공개 흐름, 서버 API, 실행과 검증.

6. [아트·필살기·서버 운영](docs/06-art-combat-and-operations.md) — 새 전투 표현, 아트 출처, 서버 비용과 배포·백업, 검증 결과.

7. [현재 콘텐츠와 자랑·성장 확장안](docs/07-progression-and-showcase-roadmap.md) — 5탄 이후 목표, 랭킹·전시·BM 우선순위와 실제 결제 준비 상태.

8. [특수 던전·길드 지원·주간 기록](docs/08-rift-guild-and-weekly-records.md) — 0.4의 실제 추가 기능과 보상·권한·호환성.

상점 거래는 NPC와의 거래로 가정한다. 유저 간 거래·경매장·현금 환전은 초기 범위에 없다. 직접 이동하는 전투를 기본안으로 두고, 완전 방치형을 원하면 전투 조작과 재화 수급표부터 변경한다.

다음 개발 단계는 첫 10분 안내 개선과 Android 실기기 성능·개조 및 지역 보상 밸런스 검증이다. 출시 전 Google 계정 연동·실제 Play 결제 검증·AdMob SSV를 연결해야 한다. 길드 공동 보스와 시즌 패스는 아직 구현하지 않았다.

9. [0.5 지역·작전 확장](docs/09-campaign-expansion.md) — 30작전·맵 판정·적 행동·기존 계정 호환·검증.
10. [유명 게임 비교와 개선 우선순위](docs/10-reference-gap-review.md) — 공식 출처, 부족한 점, 다음 개발 완료 기준.
11. [0.6 개조·분석·수집·지원 전시](docs/11-workshop-and-collection.md) — 다섯 가지 확장 기능, 생성 아트와 프롬프트, 호환성·검증.
