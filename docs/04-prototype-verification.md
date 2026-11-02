# 프로토타입 구현과 검증

2026년 11월 2일 기준 **0.2.0**. 기존 생존 전투·성장에 전시실, 카드 공유와 별도의 서버 계정을 추가했다. 실제 Google Play 결제, AdMob 광고, Google 로그인, 공개 운영 서버 배포는 아직 연결하지 않았다.

## 산출물

| 산출물 | 경로 |
| --- | --- |
| Android 개발 APK | `Builds/Android/ScrapGuard-prototype.apk` |
| macOS 미리보기 | `Builds/macOS/ScrapGuard.app` |
| 로컬 화면·게임 검증 | `TestResults/Smoke02/` |
| 서버 장애 주입·통합 검증 | `TestResults/Cloud02Final/` |
| 최종 미리보기 서버 화면 | `TestResults/Cloud02ReleaseCheck/` |
| Unity 테스트 | `TestResults/editmode-02-final.xml` |
| 빌드 로그 | `Logs/build-all-02-final.log` (macOS), `Logs/build-android-02-final.log` (Android 최종) |
| 서버 코드와 테스트 | `Server/ScrapGuard.Server/`, `Server/Checks/` |

검증 결과·빌드·로그·DB는 Git에서 제외한다. 테스트 계정과 데이터베이스는 별도 임시 폴더를 사용했고 사용자 로컬 저장 파일을 변경하지 않았다.

## 완료한 검증

| 검사 | 결과 |
| --- | --- |
| C# 핵심 규칙 | 31/31 통과. 출석·한도·강화·판매·보상·전투·업적 소유권·외형 가격·전투 재현 |
| Unity EditMode | 37/37 통과. 위 31개와 파일·백업·한글 글꼴·서버 일자 캐시·요청 기록 복구·HTTPS 주소 검증 |
| 서버 SQLite 검사 | 34개 통과. 계정 분리, 요청 ID·내용 충돌, revision 충돌, 중복 지급 차단, 공개·비공개, 응원·신고·차단, 삭제 연쇄, 전투 검증, 외형 구매 |
| 로컬 앱 실행 | 기존 10개 화면에 전시·꾸미기·도색·카드 PNG·계정 화면 추가. 출석·강화 클릭, 보스 전투, 결과 저장 재로드. 실행 오류 0 |
| 서버 앱 통합 | 계정 생성, 출석, 공개, 광장·상대 조회, 응원·차단·해제, 40초 입력 재현 정산, 다른 클라이언트의 복구. 실행 오류 0 |
| 장애 복구 | 서버가 출석 보상을 확정한 직후 응답을 강제로 끊음. 미확인 요청 파일 유지, 같은 요청 재전송, 중복 보상 없이 출석 1회 확인 |
| 공개 정보 | 재화 잔액·개인 복구 코드·전체 인벤토리 대신 표시용 DTO만 제공. 내 카드와 상대 카드 화면 확인 |

서버 통합 테스트의 Curl 52는 프록시가 의도적으로 응답을 끊은 결과다. 클라이언트가 실제로 미확인 요청을 다시 보낸 로그와 최종 상태를 확인했다. 새 Android JNI 모듈과 개발 콘솔 표시 변경을 포함해 플레이어를 빌드했다. 최종 macOS 서버 통합 실행도 오류 0개다.

## 구현 위치

- `Assets/Game/Core/`: 공유 경제·전투·전시 규칙, 명령·응답과 입력 재현 프로토콜.
- `Assets/Game/Runtime/`: 한국어 UI, 전시·공유, 서버 연결, 로컬/서버 저장 분리, 자동 실행 검사.
- `Assets/Editor/`: 프로젝트와 개발 빌드, Android FileProvider·공유 경로 구성.
- `Server/`: ASP.NET Core, SQLite WAL 거래 원장·계정·소셜과 서버 검사.
- `Tools/`: 엔진 없는 핵심 검사와 테스트용 응답 유실 프록시.

## 검증 한계와 다음 단계

Android 기기가 연결되어 있지 않아 APK의 설치·터치·공유 대상 앱·발열·실행 성능·16KB 기기 구동은 확인하지 않았다. macOS와 서버 간 전투 입력 재현은 확인했지만 Android IL2CPP와 다양한 칩셋에서의 결과 일치는 실기기 검증 대상이다.

현재 5개 구역은 공통 아레나에 난이도·보상·해금을 적용한다. 아트와 전투 밸런스 검증, Google 계정·Play 결제 검증·AdMob SSV, 신고 처리 운영 도구, HTTPS 호스팅과 백업·복구, 개인정보·스토어 자료를 추가해야 한다. 상세 범위는 [전시실과 서버](05-showcase-and-server.md)에 기록했다.

## 최종 Android 산출물 확인

- 버전 0.2.0 / 코드 2, 패키지 `com.scrapguard.mobilegame2.prototype`.
- Unity IL2CPP ARM64 개발 빌드, 최소 API 26 / target API 36.
- APK v2 서명과 16KB ZIP 정렬 통과. 포함 네이티브 라이브러리 6개의 ELF LOAD 정렬도 모두 16KB 이상.
- FileProvider와 제한된 showcase 캐시 경로 포함, 인터넷 권한 포함, 외부 저장소 쓰기 권한 없음, Android 자동 백업 비활성화.
- Android 네이티브 공유 창의 실제 기기 실행은 미검증.

크기 33,454,357바이트. SHA256 `3aa8aaf04e77772429003988a9f2000dd0ead411411a293f06824e3ac4f79d38`.

검사 재현: `python3 Tools/verify_apk.py`. 결과는 `TestResults/apk-02-verification.json`과 `apk-02-manifest.txt`에 저장한다. `SCRAP_ANDROID_PLAYER` 환경 변수로 다른 Unity Android 지원 경로를 지정할 수 있다.
