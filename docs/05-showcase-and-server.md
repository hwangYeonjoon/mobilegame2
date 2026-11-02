# 0.2 전시실과 서버 계정

## 제품 방향과 구현

성장시키는 재미 뒤에 보여 줄 장소가 있어야 한다. 전시실에서 **성장 장비**, **플레이 업적**, **취향을 나타내는 외형**을 한 카드에 모으고, 다른 사람의 프로필을 보며 다음 성장 목표를 발견하도록 구성했다.

| 요소 | 구현과 획득 조건 |
| --- | --- |
| 대표 대원 | 보유한 대원 중 선택. 출격 대원과 별도 저장 |
| 전시 장비 | 현재 착용한 3종을 전시에 복사. 강화 단계·등급 표시. 판매 시 현재 장비로 대체 |
| 칭호 | 기본 / 첫 구역 클리어 / 출석 7일 / 처치 1,000 / 구역 5개 정복 |
| 프레임 | 기본 / 첫 승리 / 출석 7일 / 태양의 테두리 250보석 |
| 도색 | 기본 / 앰버 300보석 / 서리 처치 1,000. 정비소·전투·전시에 적용 |
| 카드 공유 | 닉네임·대원·장비·기록의 PNG. 로컬 체험 또는 서버 ID를 구분 표시 |
| 전시 광장 | 기본 비공개, 동의 후 공개. 목록·ID 검색·응원·신고·양방향 차단·차단 관리 |

프레임과 도색은 능력치를 바꾸지 않는다. 현재 보석은 게임 내 획득분이며 실제 충전은 준비 중이다. 보유한 꾸미기 상품을 다시 결제할 수 없고, 잠긴 칭호·도색·대원을 강제 선택하는 요청도 거절한다. 응원에는 재화·전투 보상이 연결되지 않는다. 랭킹, 길드, 채팅, 사용자 업로드 이미지, 확률형 뽑기는 이번 단계에 넣지 않았다.

Android 공유는 앱 캐시의 `showcase/` PNG에만 FileProvider URI 권한을 부여해 시스템 공유 창을 연다. 저장소 전체 권한은 요청하지 않는다. 사용자가 공유 대상 앱을 선택해야 전송된다. macOS는 PNG를 임시 캐시에 저장한다. 공유 이미지에 지갑·아이템 인스턴스 ID·복구 코드는 없다. [Android FileProvider 문서](https://developer.android.com/reference/androidx/core/content/FileProvider)

## 서버 실행

```sh
dotnet run --project Server/ScrapGuard.Server/ScrapGuard.Server.csproj
```

.NET 10 SDK 필요. 기본 `127.0.0.1:5187`에만 수신하고 프로젝트의 `data/scrapguard.db`에 저장한다. `SERVER_URLS`와 `GAME_DB` 환경 변수 또는 같은 이름의 명령행 설정으로 주소·DB 경로를 바꿀 수 있다. DB·WAL·공유 메모리 파일과 클라이언트 복구 코드를 Git에 올리지 않는다.

USB Android 테스트는 연결 후 `adb reverse tcp:5187 tcp:5187`을 실행하고 게임의 설정 → 계정·서버 연결에서 `http://127.0.0.1:5187`을 사용한다. 에뮬레이터에서는 `10.0.2.2`를 사용할 수 있다. 일반 HTTP 원격 서버는 앱이 거절하며 원격 운영은 HTTPS가 필요하다.

현재는 **개인 복구 코드 기반 게스트 계정**이다. 256비트 난수 코드를 생성하고 서버에는 해시만 저장한다. 별도의 공개 ID는 인증 자격이 아니다. 복구 코드를 가진 사람은 계정에 접근할 수 있으므로 사용자에게 비밀로 보관하도록 안내한다. Google 로그인, 코드 회전·분실 복구, 기기별 세션 철회, 안전한 플랫폼 자격 증명 저장은 출시 전 추가 대상이다. 프로토타입 클라이언트 코드는 앱 내부 JSON에 저장한다. Android 자동 백업은 비활성화했다.

서버 계정은 로컬 체험의 테스트 잔액을 가져오지 않고 새로 시작한다. 서버가 끊기면 마지막 캐시를 표시하되 로컬 방식으로 재화를 확정하지 않는다. 로컬 체험으로 전환해도 서버 계정은 삭제되지 않는다. 같은 서버 주소와 복구 코드로 다시 연결할 수 있다.

## 구현 API

| 경로 | 역할 |
| --- | --- |
| `GET /health` | 버전과 실제 결제 미연결 상태 |
| `POST /v1/auth/guest` | 난수 secret으로 등록, 동일 secret 재시도는 같은 계정 |
| `GET /v1/bootstrap` | 서버 시각·계정 상태·revision·진행 중 작전 |
| `POST /v1/commands` | 출석·임무·상점·장비·캐릭터·전시 설정·전투 거래 |
| `GET /v1/profiles?after=` | 공개 프로필 페이지. 최대 20개, next 커서 |
| `GET /v1/profiles/{id}` | 공개 프로필 또는 내 프로필 |
| `POST /v1/profiles/{id}/like` | enabled로 응원 설정. 중복 추가 없음 |
| `POST /v1/profiles/{id}/report` | name / cheating / other 신고 접수 |
| `POST /v1/profiles/{id}/block` | enabled로 차단·해제. 양쪽 조회 차단과 응원 제거 |
| `GET /v1/blocks` | 내가 차단한 공개 ID 목록 |
| `DELETE /v1/account` | 계정·원장·거래 요청·소셜 관련 행 삭제 |

인증 요청은 `Authorization: Bearer <개인 복구 코드>`를 사용한다. 일반 거래는 `GameCommand`의 `requestId` 32자리, `revision`, `type`과 해당 매개변수로 요청한다. 클라이언트가 가격이나 지갑 상태를 업로드하는 경로는 없다.

## 거래와 전투 보상

- 서버의 SQLite 트랜잭션에서 상태, revision, 요청 해시·메시지, 재화 증감 원장을 함께 기록한다. 같은 요청 재시도는 다시 지급하지 않고 현재 상태를 반환한다. 같은 ID에 다른 본문 또는 다른 기기의 오래된 revision은 409다.
- 앱은 변경 요청을 보내기 전에 정확한 본문을 파일에 기록한다. 연결 실패·5xx·429·408은 이 기록을 유지한다. 재연결 때 같은 요청을 재전송한다. 서버 확정 후 응답이 유실돼도 잔액을 추정해 더하지 않는다.
- 출석과 일일 한도는 서버 시각의 한국 오전 5시 기준이다. 클라이언트 캐시는 기기 시각으로 일일 카운터를 초기화하지 않는다.
- 전투 시작 시 서버가 시드·세션·장비 스냅샷을 보관한다. 앱은 이동을 30Hz로 양자화·기록하고 스킬 선택도 기록한다. 종료 시 서버가 재현한 처치·생존 시간·승리로 보상한다.
- 실제 경과 시간보다 긴 입력, 불가능한 스킬, 잘못된 축·프레임, 과도한 기록은 거절한다. 전투 중 기기 종료 후 이어하기는 미구현이며 계정 화면에서 중단 작전을 보상 없이 종료할 수 있다. 이미 제출한 미확인 종료 요청은 먼저 동기화한다.

서버와 Unity가 `Assets/Game/Core/*.cs`를 공유한다. 재현은 수정 클라이언트나 자동 조작을 완전히 방지하지 않는다. 서버 기록이라는 표시는 경쟁 무결성 보증이 아니다. 실제 경쟁 기능을 넣기 전 서버 설정 버전, 재현 CPU 예산, 봇 탐지와 여러 Android 칩셋의 재현 일치성을 추가 검증해야 한다.

## 검증 재현

```sh
dotnet run --project Tools/CoreChecks/CoreChecks.csproj
dotnet run --project Server/Checks/Checks.csproj
unity test . --mode EditMode --output TestResults/editmode-02-final.xml -- -nographics
```

서버 검사는 임시 SQLite DB를 만들고 삭제한다. 단일 지급·요청 내용 충돌·revision 충돌·테스트 지급 차단·서버 시각·공개 데이터 제외·응원·차단·신고·삭제 연쇄·전투 입력 재현·외형 구매를 검사한다.

런타임 통합 검사는 테스트 서버에만 실행한다. `Tools/cloud_smoke_proxy.py`는 localhost:5188에서 첫 출석 요청의 **서버 확정 이후 응답을 끊는다**. 앱 실행 인자는 `-smoke -cloudSmoke -server http://127.0.0.1:5188 -profilePath <별도 임시 폴더>/profile.json -smokeOutput <결과 폴더>`다. 기존 실계정 경로를 사용하면 안 된다. 프록시가 발생시킨 Curl 52는 의도한 장애이며, 앱의 미확인 기록 재전송과 최종 잔액을 검증한다. 이 프록시는 운영 서비스가 아니다.

## 공개 운영과 결제 전에 남은 작업

- Google 계정 연동, 자격 증명 저장·철회·분실 복구, HTTPS 운영 호스트와 백업·복구 절차.
- Google Play 구매 검증·consume·acknowledge·환불·유료/무료 보석 분리, AdMob SSV·광고 스킵 권리.
- 공개 운영을 위한 닉네임 필터, 신고 처리·제재 관리자 기능, 계정 삭제 웹 경로와 개인정보 안내. 현재 신고는 SQLite 접수까지이며 자동 제재는 없다.
- SQLite 단일 프로세스·전역 쓰기 잠금과 재현 비용의 부하 검증, 원장/요청 기록 보관 정책, 서버 상태·이상 경제 이벤트 관찰.
- 앱을 재빌드하는 동안의 활성 전투 설정 호환성, 실기기 전투 재현과 Android 공유 창 수신 앱 검증.

데이터베이스 패키지는 Microsoft.Data.Sqlite 10.0.14와 SQLitePCLRaw.bundle_e_sqlite3 3.0.5를 고정했고 테스트에서 네이티브 SQLite 3.53.4를 확인했다. [Microsoft 패키지](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.14), [SQLite 번들 패키지](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/3.0.5).

Android 공유 의존성은 AndroidX Core 1.15.0이다. Unity GameActivity의 Kotlin JDK7/8 1.6 계열과 새 stdlib의 클래스 중복을 해결하기 위해 Kotlin BOM 1.8.22로 런타임 버전을 맞췄다. Kotlin 컴파일러 플러그인을 추가한 것은 아니다. [Kotlin 표준 라이브러리 버전 정렬](https://kotlinlang.org/docs/gradle-configure-project.html#versions-alignment-of-transitive-dependencies).
