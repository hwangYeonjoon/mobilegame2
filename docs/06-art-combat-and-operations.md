# 0.3 아트·필살기·소규모 서버 운영

## 2026-11-05 UI·이미지 보강 — 로컬 후속 버전

장비 12종, 스킬 8종, 보석 상품/광고 스킵 5종을 새 투명 배경 아틀라스로 연결했다. 장비는 일반=회색/I, 고급=민트/II, 희귀=파랑/III를 `InventoryArt`에서 공통 적용한다. 슬롯·보유·장착·잠금 상태는 텍스트로도 표시한다. 장비 상세는 큰 그림을 제공하고 구매 비교는 실제 `Catalog.AttackFor`/`HealthFor`, 현재/변경 효과, 같은 등급 보유 수량을 사용한다. 모듈에 임의 공격력 보너스를 표시하지 않는다.

| 파일 | 구성 | 생성 프롬프트 |
| --- | --- | --- |
| `Assets/Resources/Art/EquipmentIcons-v1.png` | 4×3, 카탈로그 장비 순서 12종 | `ArtSource/EquipmentIcons-v1.prompt.txt` |
| `Assets/Resources/Art/SkillIcons-v1.png` | 4×2, 카탈로그 스킬 순서 8종 | `ArtSource/SkillIcons-v1.prompt.txt` |
| `Assets/Resources/Art/ShopPacks-v1.png` | 5×1, 보석 300/600/1200/2600/영구 광고 스킵 순서 | `ArtSource/ShopPacks-v1.prompt.txt` |

생성 도구는 imagegen 내장 도구. 원본은 기존 생성 디렉터리에 보존하며 파일명은 각각 `exec-6dc37399-14e8-415b-a475-0fb08eaa00ae.png`, `exec-e2cd4a8c-3391-419a-aadf-306c88adabfa.png`, `exec-2702ac76-78bd-4cb0-81e9-64b2c8cf3519.png`이다. 상점 그림은 최초 출력의 흰 배경을 같은 도구로 제거한 결과다. 지급 수량·상품 ID·Google Play 가격 처리와 기존 경제 규칙은 유지한다. 그림 속 보석의 개수는 지급 수량을 뜻하지 않으며 상품 이름에 확정 지급 수량을 표시한다.

메뉴는 안전 영역의 전체 높이를 사용한다. 정비소의 작전명·이전/다음 선택·출격은 고정 하단에 함께 표시해 짧은 화면에서도 작전을 확인하고 출격할 수 있다. 긴 정비소·상점·전시실은 스크롤하고, 장비/상품/의상/스킬은 카드 전체 선택을 지원한다. 장비 상세, 의상 체험, 메뉴 이동 버튼도 확대했다. 전투의 540×960 시뮬레이션 좌표와 리플레이 규칙은 그대로다. 정비소·의상 추천·전시 카드에는 공통 정비대, 조명, 받침대를 추가했다. 전시 카드는 대표 모습·칭호·작전 정복을 우선 표시하고 장비·공격/체력·처치/출석은 별도 상세에서 확인한다. 공개 프로필에도 같은 상세 접근을 제공한다. 스크롤 위치와 무관하게 전체 전시 카드를 복사하여 공유 캡처한다.

의상 목록은 두 열 썸네일, 시리즈·대원·보유·장착 가능 조건을 함께 적용한다. 빈 결과에 초기화 동작을 제공한다. 작은 전투 크기에서는 의상별 장식을 더 적고 굵은 실루엣으로 바꾸며 피팅룸도 동일하게 적용한다. 몬스터 외곽선, 움직임에 따른 플레이어 그림자, 구조물의 드리운 그림자, 맵 가장자리 명암과 최대 12개의 낮은 대비 환경 입자를 보강했다. 환경 효과는 시뮬레이션 시간으로 갱신하며 위험 표식 아래에 남는다.

기본/확장 대원 전체의 새 연속 걷기·피격 스프라이트 제작은 이번 변경에 포함하지 않는다. 기존 자세 전환과 반동·기울기 동작을 유지하고 전투 크기 가독성을 보강한 범위다. Android 실제 터치 편의·장시간 성능·구매 전환 효과는 별도 실기기/사용자 검증 대상이다.


검증: Unity macOS 개발 빌드 성공. `TestResults/UiCompleteTall`(360×800), `TestResults/UiCompleteShort`(360×640) 각각 PNG 35개, 런타임/클릭 검사 오류 0건. 신규 그림 로드, 메뉴 안전 영역, 고정 출격 버튼, 구매 비교의 상태 불변, 장비 구매 차감, 의상 필터/빈 상태, 체험의 상태 불변, 의상 구매→장착→전시, 보석 부족 구매 차단, 스크롤 후 전체 카드 공유, 스킬 선택/도감/HUD, 7개 지역 표현을 확인했다. 전투·재화는 격리된 로컬 테스트 조건이며 실결제/운영 서버 검증이 아니다. `BattlePresentationSpecification` 4개 통과(`TestResults/ui-battle-presentation.xml`): 전체 42작전 충돌 경계 일치, 파티클 수명/상한, 적 오브젝트 재사용 상태, 특수 던전 바닥/장애물 분리. Android 실기기 검증을 대체하지 않는다.

## 이번 구현

캐릭터는 수집하고 싶은 장난감 로봇 방향을 유지한다. 어린이 전용 타깃은 아직 확정하지 않았으며, 청소년·성인 중심의 수집형 게임을 작업 가정으로 사용했다. 상점에서 도색을 크게 보고 모든 대원에게 입혀 볼 수 있다. 미보유 대원 미리보기는 구매나 능력치 변경을 일으키지 않는다. 도색은 기존 확정 가격과 업적 획득 구조를 유지한다.

몬스터는 녹슨 수집 로봇·보라색 고속 로봇·중장갑 정예·폭주 코어 보스 4종에 각각 4프레임 이동 아트를 연결했다. 바닥은 금속 패널과 배관이 있는 정유 공장 아트로 교체했다. 5개 구역은 같은 지형의 색상 변형이며 서로 다른 맵 구조나 장애물 충돌은 아직 없다.

플레이어는 이동 시 상하 움직임·기울기·좌우 방향 전환, 사격 시 반동·총구 섬광, 피격 시 색 변화가 있다. 캐릭터 자체의 뼈대 애니메이션이나 별도 걷기 프레임은 아직 없다. 탄환 코어, 보스 공격 경고 테두리, 보호막, 필살기 파동·광선과 효과음을 추가했다.

| 대원 | 필살기 | 전투 규칙 |
| --- | --- | --- |
| 루크 | 고철 폭풍 | 반경 245의 광역 공격, 공격력 5배, 밀치기 |
| 레이 | 광자 관통 | 가까운 적 자동 조준, 길이 560·반폭 38 관통 광선, 공격력 12배 |
| 볼트 | 요새 강하 | 반경 180 충격파, 공격력 7배, 최대 체력 35% 보호막 5초 |

범위 판정에는 적의 충돌 반경이 포함된다. 시작 충전 50%, 초당 2%, 처치당 2%를 얻는다. 100%에서 우측 하단 버튼 또는 PC Space로 발동하며, 사용 후 재사용 대기시간은 16초다. 스킬 선택·일시정지·종료 중에는 발동하지 않는다. 세 필살기 모두 발동 직후 약 0.55초 동안 피해를 막는다. 이 수치는 첫 프로토타입 값이며 실기기 플레이와 밸런스 검증이 더 필요하다.

필살기 발동은 이동 입력 사이의 별도 이벤트로 기록한다. 서버가 같은 C# 전투 규칙으로 충전·피해·처치 결과를 재현한다. `rulesVersion=3`을 티켓과 요청에 넣으며 이전 클라이언트는 새 전투를 시작할 수 없다. 기존 DB에는 `run_rules` 컬럼을 추가하고 진행 중인 이전 전투는 정산을 거절한다. 계정 화면에서 중단 전투를 취소한 뒤 새 작전을 시작할 수 있다.

## 아트 출처와 파일

`imagegen` 스킬의 **built-in tool mode**로 생성했다. CLI/API 키 방식은 사용하지 않았다. 생성 원본을 수정하지 않고 프로젝트로 복사했으며, 투명 배경 아틀라스는 런타임 Sprite 영역으로 나눈다. 최종 상업 아트 검수는 별도로 필요하다.

| 프로젝트 파일 | 용도 | 실제 생성 프롬프트 |
| --- | --- | --- |
| `Assets/Resources/Art/Guardians-v1.png` | 3대원 × 3도색 | `ArtSource/guardians-v1.prompt.txt` |
| `Assets/Resources/Art/Enemies-v1.png` | 4종 × 4이동 프레임 | `ArtSource/enemy_atlas-v1.prompt.txt` |
| `Assets/Resources/Art/Refinery-v1.png` | 전투 바닥 | `ArtSource/arena_floor-v1.prompt.txt` |

원본 생성 디렉터리: `/Users/kd-server/.codex/generated_images/01a19a0d-09de-75a1-ac02-222a71ac93fb/`. 원본 파일명은 순서대로 `exec-7980e27f-ecae-4bd0-9d77-8f88b76b948e.png`, `exec-e1e98616-0229-4818-9f44-5ca2c6318696.png`, `exec-43a26129-65f7-4f34-b912-7e9394786eb2.png`다. 앱은 프로젝트 안의 파일만 참조한다. Android는 ETC2 RGBA8, 최대 2048, mipmap 없음으로 가져온다.

어린이를 포함하는 실제 타깃을 정하면 광고 SDK·광고 타기팅·결제 유도·소셜 노출을 그 타깃에 맞춰 다시 설계해야 한다. 귀여운 그림과 스토어 연령 표시만으로 타깃을 구분할 수는 없다. 현재 실제 결제와 광고는 연결하지 않았다. 기준: [Google Play Families](https://support.google.com/googleplay/android-developer/answer/9893335).

## 공개 서버 권장안

첫 비공개 베타는 **서울 리전의 작은 VM 한 대 + Docker Compose + Caddy HTTPS + API + 영속 SQLite**로 시작한다. 초기에는 실시간 멀티플레이 서버가 필요하지 않다. 전투는 기기에서 실행하고 서버는 계정·경제·전시·입력 재현 정산을 담당한다.

AWS Lightsail Linux 공인 IPv4 요금표 기준 2GB/2vCPU는 월 US$12, 4GB/2vCPU는 US$24다. 우선 4GB로 서버 빌드와 운영 여유를 확보하는 안을 권한다. 도메인·백업·초과 전송량·세금은 별도이며 동시접속 수를 보장하는 산정은 아니다. 서버 검증 전투가 CPU를 사용하므로 부하 테스트 후 규모를 정해야 한다. [AWS Lightsail 가격](https://aws.amazon.com/lightsail/pricing/).

유료 출시 또는 서버 여러 대가 필요해지면 PostgreSQL과 관리형 백업/PITR로 옮기고 계정 인증·결제 검증을 먼저 보강한다. 현재 저장소는 프로세스 내부 잠금과 SQLite 파일 하나를 사용하므로 API를 수평 확장하지 않는다. Cloud Run 같은 서비스로 이동할 경우 로컬 SQLite 파일을 그대로 저장소로 사용할 수 없다. [Cloud Run 파일 시스템 수명](https://docs.cloud.google.com/run/docs/container-contract#file_system).

## 배포 준비

`Server/Deploy/compose.yaml`은 공개 운영 준비용 템플릿이다. 이번 작업에서는 외부 VM, DNS, 인증서, 유료 리소스를 만들지 않았다.

1. VM에 Docker Engine/Compose를 준비하고 API 도메인의 A 레코드를 공인 IPv4로 연결한다. 실제 IPv6를 설정하지 않았다면 AAAA 레코드는 넣지 않는다.
2. 방화벽은 80/443을 공개하고 SSH는 운영자 IP로 제한한다. API 8080이나 SQLite는 외부에 공개하지 않는다.
3. 프로젝트를 서버에 복사하고 다음처럼 환경 파일을 준비한다.

```sh
cd Server/Deploy
cp .env.example .env
chmod 600 .env
```

`.env`의 `GAME_DOMAIN`, `ACME_EMAIL`, `BETA_INVITE_CODE`를 실제 값으로 바꾼다. 초대 코드는 16~80자이며 예제의 `replace-` 값으로 서버를 시작하면 실패한다. 생성 예: `openssl rand -hex 24`. 초대 코드와 게스트 개인 복구 코드는 다른 용도다. 신규 가입은 기본 닫힘 상태다.

```sh
docker compose --env-file .env config --quiet
docker compose up -d --build
curl --fail https://YOUR_API_DOMAIN/health
curl --fail https://YOUR_API_DOMAIN/health/ready
```

HTTPS와 백업·복구 확인 후 `.env`의 `REGISTRATION_ENABLED=true`로 바꾸고 `docker compose up -d api`로 재생성한다. 앱의 설정 → 계정·서버 연결에 HTTPS 주소와 초대 코드를 입력한다. 기존 계정 복구는 가입 종료 후에도 가능하다.

API는 비루트 사용자, 읽기 전용 루트 파일 시스템, 제한된 임시 파일 시스템과 capability로 실행한다. Caddy만 외부 포트를 받는다. 신뢰 프록시는 고정 사설 IP `172.31.248.2` 한 개다. 이 대역이 호스트 네트워크와 겹치면 Compose 대역과 `TRUSTED_PROXY_IPS`를 함께 바꾼다. [ASP.NET Core 프록시 설정](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0), [Caddy 전달 헤더 기본 동작](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy#defaults).

IP별 분당 180요청, 신규 등록은 별도로 분당 10요청으로 제한한다. 전투 명령 등 `/v1/commands`에는 동시 처리 제한 2개와 대기열 8개를 둔다. 이는 초기 방어 장치이며 부하 테스트나 DDoS 보호를 대체하지 않는다. 전시 신고는 DB 접수까지 구현되어 있으므로 공개 모집 전에 운영자가 신고를 확인하고 처리하는 절차가 필요하다.

## 백업·복구·점검

배포 디렉터리에서 `sh backup.sh`를 실행하면 실행 중인 DB의 일관된 스냅샷을 만들고 무결성과 외래키를 검증한 뒤 `backups/`로 내보낸다. WAL 파일을 제외한 단순 파일 복사는 사용하지 않는다. SQLite 온라인 백업 중에는 잠시 쓰기가 막힐 수 있어 트래픽이 적을 때 실행한다. [SQLite Backup API](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup).

운영자는 하루 1회 이상 자동 실행, 별도 서버/암호화 오브젝트 스토리지 복사, 보관 기간과 복구 훈련을 설정해야 한다. 이 원격 백업 자동화와 알림은 아직 연결하지 않았다. VM 안의 백업 볼륨만으로는 VM 손실에 대비할 수 없다. 일일 백업만 사용하면 최대 하루 진행 손실 가능성이 있다.

복구는 우선 별도 테스트 DB/볼륨으로 한다. 아래 CLI로 파일을 검증하고, 복사한 백업을 `GAME_DB`로 지정한 별도 API에서 계정·재화·원장을 확인한다.

```sh
docker compose exec -T api dotnet ScrapGuard.Server.dll --verify-backup /backups/SELECTED_BACKUP.db
```

실제 교체 시에는 신규 가입을 닫고 `MAINTENANCE_MODE=true`로 API를 재생성해 쓰기를 막는다. 현재 DB도 추가 백업한다. 그 뒤 API를 완전히 정지하고 새 볼륨에 검증된 백업을 배치한다. 이전 DB의 `-wal`/`-shm` 파일을 새 백업과 섞지 않는다. 새 볼륨을 API 사용자에게 쓰기 가능하게 연결하고 점검 모드로 재기동한다. `/health/ready`와 계정 읽기를 확인한 뒤 점검 모드를 푼다. 이전 볼륨은 복구 확인 전까지 보존한다. 백업 이후 거래는 되돌아갈 수 있으므로 출시 후 결제 원장과 복원 처리를 포함한 별도 절차가 필요하다.

모니터링은 외부 `/health/ready`, 컨테이너 재시작/메모리, 디스크 사용량, 5xx/429, 백업 성공 시각을 대상으로 연결한다. 현재 알림 서비스는 연결하지 않았다. Docker 로그는 파일당 10MB·3개로 제한했다. 배포 이미지는 검증된 태그/다이제스트를 기록해 교체하고 정기 보안 업데이트 때 다시 검증한다.

## 검증

추가로 엔진 시작 로고를 끄고 생존·성장·전시를 소개하는 게임 타이틀을 넣었다. `TestResults/Title03/00-title.png`와 `report.txt`에서 표시·버튼 진입 오류 0을 확인했다. Android OS 시작 화면은 실기기 미검증이다.

- 순수 C# 핵심 규칙 35개: 필살기 조건·범위·보호막 만료·3대원 재현·조작 기록 거절 포함.
- 서버 검사 50개: 이전 전투 버전 차단·DB 마이그레이션·초대 코드·백업 복원 포함.
- Unity EditMode 43개 통과: 모든 캐릭터/몬스터 아틀라스 영역과 런타임 직렬화 포함.
- macOS 로컬 플레이 자동 확인: 화면 21개, 버튼 차단 검사·구매 미리보기 무변경·필살기 사용·저장 확인, 오류 0.
- Unity → Linux Docker 서버 연동: 필살기 2회 사용 전투의 처치/시간 재현·정산·계정 복구, 오류 0.
- Docker 배포 검사 10개: HTTP 가입 제한·중복 거래·실행 중 백업·내보낸 DB 계정 확인·컨테이너 재시작 복구.
- Compose 및 Caddy 구성 검증 통과. 실제 공인 DNS/TLS 발급과 공개 네트워크 부하 검증은 하지 않았다.
- Android APK 0.3.0(code 3) 빌드·v2 서명·ZIP/ELF 6개 16KB 정렬 검증 통과. 크기 54154801 bytes, SHA-256 `e4179bb26417e8a7de79f3e0a476b1911b9442869d7a1fe4ecbc9bd7b13fb940`. 상세 결과는 `TestResults/apk-03-verification.json`에 기록했다. Android 실기기는 연결되지 않아 실제 터치·성능·발열은 미검증이다.

재실행:

```sh
dotnet run --project Tools/CoreChecks/CoreChecks.csproj
dotnet run --project Server/Checks/Checks.csproj
docker compose -f Server/Deploy/compose.local.yaml up -d --build
python3 Tools/deploy_checks.py
```

로컬 Compose는 `127.0.0.1:5190`만 열고 운영용 볼륨과 분리된다. 검증 종료 후 테스트 컨테이너는 정지했으며 볼륨과 백업은 보존했다. `local-integration-test-invite-03`은 이 테스트 스택 전용 공개 테스트 코드이며 운영에 사용하지 않는다.
