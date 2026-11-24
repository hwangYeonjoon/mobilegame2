# 판매·광고 검증과 0.40.1 출시 요청 — 2026-11-24

## 현재 상태

- 확인된 공개 앱 버전: 0.40.0 / code 43 / 전투 규칙 30. 아래 RTDN 보완을 포함한 서버 이미지는 `scrapguard-api:0.40.1`로 반영했다.
- 새 소스와 업로드본: **0.40.1 / code 44 / 규칙 30**. 설치 아이콘을 feature graphic의 대표 로봇과 같은 분위기로 교체했다. 전투·경제·가격·저장 규칙은 변경하지 않았다.
- Play Console에 44 프로덕션 전체 출시, 기본 스토어 아이콘, 첫 휴대전화 스크린샷 **3개 변경사항을 검토 요청**했다. 화면은 `검토 중인 변경사항`이며 자동 사전 검사 진행 중이었다. 공개 완료로 기록하지 않는다.
- 관리형 게시 사용 중지. Google 승인 전까지 `/v1/release` latest/minimum=43, playAvailable=true를 유지한다. 44가 실제 게시되기 전에 강제 업데이트하지 않는다.

## 상품 판매와 배포 설정 수정

Google Play 상품 5종 모두 `standard` 판매 옵션 ACTIVE, 대한민국 AVAILABLE을 확인했다. 가격을 변경하지 않았다.

| 상품 | 대한민국 가격 |
|---|---:|
| gems_300 | 2,200원 |
| gems_600 | 3,900원 |
| gems_1200 | 7,500원 |
| gems_2600 | 14,900원 |
| ad_skip_lifetime | 9,900원 |

운영과 로컬 비공개 배포 환경에서 COMMERCE_MODE=production, COMMERCE_RELEASE_READY=true, COMMERCE_SALES_ENABLED=true, COMMERCE_DISABLED_PRODUCTS는 명시적 빈 값이다. `/v1/commerce` salesEnabled=true, licenseTest=false, saleProducts 5종을 확인했다.

Compose의 `${COMMERCE_DISABLED_PRODUCTS:-ad_skip_lifetime}`는 명시적으로 빈 값을 넣어도 기본 제한을 되살렸다. `${COMMERCE_DISABLED_PRODUCTS-ad_skip_lifetime}`로 수정해 **미설정일 때만 안전 기본값**을 적용한다. 이 수정은 운영 Compose에도 반영했다. 미설정/빈 값/명시 목록 3가지 검사를 추가했다.

## 광고 설정과 Google 인증을 구분

- 클라이언트 adsEnabled=true, productionAds=true.
- AdMob 앱 ID: `ca-app-pub-6176006174123307~8500016434`.
- 보상형 단위: `ca-app-pub-6176006174123307/7008464653`.
- 운영 ADS_MODE=production, ADS_RELEASE_READY=true. `/v1/ads` enabled=true, mode=production 및 동일 단위를 확인했다.
- AdMob 단위의 서버 검증 URL: `https://api.scrapguard.win/v1/ads/ssv`.
- 공개 Play 개발자 웹사이트는 `https://api.scrapguard.win`. `/app-ads.txt`는 HTTP 200, 텍스트 응답이며 AdMob에서 직접 복사한 `google.com, pub-6176006174123307, DIRECT, f08c47fec0942fa0`와 일치한다.
- AdMob `업데이트 확인` 재시도 후에도 앱 인증은 실패/검토 필요 상태다. 파일 목록에는 고철 수호대의 크롤링 행이 아직 없으며 다른 앱의 승인 표시를 이 앱의 승인으로 해석하지 않는다.
- **서버·앱 광고 요청 연결과 실제 테스트 광고 지급은 확인했지만, AdMob 앱 승인·정상 운영 송출·광고 수익 발생은 미확인**이다. Google 측 반영 지연 가능성이 있으나 인증 실패의 원인을 지연으로 확정하지 않는다.

[Google 안내](https://support.google.com/admob/answer/9675354?hl=en)는 스토어 웹사이트 변경 감지 및 파일 확인에 24시간이 걸릴 수 있고, 최근 웹사이트 변경 뒤 확인이 지연되면 일주일 후 추가 지원을 요청하도록 안내한다. [크롤러 규칙](https://support.google.com/admob/answer/9363762?hl=en)에 따라 `api.scrapguard.win`은 첫 단계 하위 도메인으로 확인 대상이다. 현재 파일과 게시자 코드를 이유 없이 바꾸지 않는다.

## 실제 Google 테스트 결과

### 구매·복원·광고 스킵

Google Play에서 설치한 정식 code 43, 로그인된 Play 테스트 계정으로 수행했다. 결제창에 **Test card, always approves / 실제 청구되지 않는 테스트 주문** 표시를 확인했다. 운영이 테스트 결제를 실결제로 받아들이지 않도록 별도 DB의 일시적 라이선스 QA 서버에서 검증했다.

1. gems_300 테스트 주문 성공 → 보석/유료 보석 각각 300, testProgress=true.
2. ad_skip_lifetime 테스트 주문 성공 → adSkip=true.
3. `광고 없이 받기` → 골드 1,800→2,400, 부품 30→38, 일일 사용 횟수 0→1. 광고 표시 없이 정상 지급.
4. 앱 강제 종료·재실행 후 Play 구매 복원 → 보석 300, 골드 2,400, 부품 38, adSkip=true 유지. 중복 지급 없음.

실제 Google 테스트 주문은 위 **2종**만 수행했다. 나머지 보석 3종은 Play 상품 메타데이터 확인과 서버 5종 지급·재요청·DB 재시작 자동 검사 범위다. 실제 카드 청구와 실제 환불 주문은 실행하지 않았다.

### 보상형 광고와 SSV

별도 QA Android 에뮬레이터에 서명된 정식 code 44 APK를 설치하고 신규 검증 계정을 운영 서버에 연결했다. SDK 동의 준비 로그 `SCRAP_AD_CONSENT ready=True error=none`을 확인했다.

- 실제 AdMob 화면 상단의 **Test Ad**를 확인하고 시청을 완료했다. 광고 상품 링크는 클릭하지 않았다.
- 운영 DB 읽기 전용 확인: parts 보상 요청은 pending에서 **granted**로 변경됐고 골드 600, 부품 8이다. 사용자·거래·토큰 값을 기록물에 노출하지 않았다.
- 앱 화면: 골드 **1,800→2,400**, 부품 **30→38**, `오늘 1/2회`로 변경됐다.
- 클라이언트 광고 콜백은 직접 재화를 지급하지 않으며, Google 서명 검증을 통과한 SSV 결과로만 보상한다. 직접 DB 보상 주입이나 수동 가짜 SSV는 사용하지 않았다.
- DB의 test=0은 **서버의 production 광고 모드**를 뜻한다. 에뮬레이터 화면의 Test Ad를 유료 광고 송출로 해석하지 않는다.

## 빌드·자동 검증

- .NET 코어 **137/137**, 최종 서버 **452개**, Compose 보간 **3개** 통과.
- `Tools/build_signed_aab.py --production` 성공, 기존 업로드 키 유지, bundletool validate 통과.
- AAB: `Builds/Android/ScrapGuard-production.aab`, 106,100,978 bytes.
- AAB SHA-256: `46743fbb45a47d7faf06e10154f7c3fc63312e5dd0c6c5f2ead6f9882ebeb10c`.
- Universal APK: `Builds/Android/ScrapGuard-production-44.apk`, 106,508,367 bytes.
- APK SHA-256: `480805296ec7ad48c978340236d9e34f7e8f17a5673f872a560005745fb5013b`.
- 패키지, 버전, SDK26/36, ARM64, v2 서명, 16KB ZIP/ELF 정렬, AdMob 앱 ID, FileProvider, 백업 비활성 검사 통과.
- APK 안의 launcher/round 아이콘 모두 새 로봇 이미지임을 추출해 확인했다. 별도 QA 기기 실제 설치 버전 44 확인, 시작 화면·신규 서버 연결·홈·상점·테스트 광고 지급 실행 성공.
- QA 기기 저장 공간 부족은 이전 APK를 백업하고 `pm uninstall -k`로 데이터를 유지한 채 실행 파일을 교체해 해결했다. 로그인된 원래 Play 기기의 앱은 삭제하지 않았다.
- Play 가독화 파일·네이티브 심볼 경고 2개는 남아 있다. 기존과 같은 세로 화면 전용 설계이고 지원 기기 수 변화가 없어, 하드웨어 범위 안내에는 이 사유를 기록하고 진행했다.
- 이번 실행은 에뮬레이터 검증이며 실물 폰 성능 검증이 아니다.

## 데이터 보호와 정리

- 변경 전 운영 DB·키 백업 `/backups/scrapguard-pre-sales-ads-20261124.db` 및 키 보관, 무결성 확인. 운영 env/Compose 변경 전 파일도 별도 보관했다.
- 테스트 결제 전용 컨테이너를 중지하고 임시 공개 QA 경로를 제거했다. Caddy는 원래 파일과 일치하는지 비교한 후 검증·재시작했다. QA 기록 볼륨은 감사용으로 남겼다.
- 원래 Play 에뮬레이터는 기존 Google 계정으로 운영 계정 복구를 마쳤다. 기존 사용자 재화나 구매 기록을 수정·초기화하지 않았다.
- 인증 키·복구 코드·구매 토큰·서명 비밀번호·비공개 배포 환경은 Git에 넣지 않는다.

## 최종 점검에서 발견한 RTDN 재시도 문제

Google RTDN은 서버의 실판매/테스트 모드가 아니라 **앱 패키지 전체**의 알림을 전달한다. 격리 QA의 테스트 주문 2건도 운영에 도착했으며, 운영 모드와 주문의 test 표지가 다르다는 이유로 pending 재시도에 남아 있었다.

- RTDN 처리에서는 Google 조회가 확인한 주문의 test 문맥을 사용한다. 기존 구매 원장의 상품·계정 바인딩·test 표지 일치 검사는 그대로 유지한다.
- 이 경로는 구매를 새로 지급하지 않고 환불 상태만 정리한다. 실판매 checkout의 테스트 결제 거부는 유지한다.
- 자동 검사: 운영에서 검증된 테스트 구매 알림 처리 시 재화/소유권 미지급, 테스트 취소 알림 정상 완료, 기존 실구매와 test 문맥이 다르면 환불 거절·재시도 유지.
- 교체 전 추가 백업 `/backups/scrapguard-pre-rtdn041-20261124.db` 및 키 무결성을 확인했다. 큐 2건의 상태·암호화 자료를 삭제하지 않고 next_attempt만 현재로 앞당겨 정상 Google 검증기로 재시도했다.
- 첫 서버 교체에서는 기존 `latestBuild >= 서버 빌드` 검사가 공개 앱 43/서버 44 조합을 막아 시작하지 못했다. 바로 기존 0.40.0 이미지로 복구했다. 이후 공개 버전과 서버 빌드를 분리하도록 수정하고, 최소 빌드·전투 규칙 검사가 유지되는 회귀 검사와 **분리 컨테이너 시작/공개 정책 검사** 후 재배포했다. 짧은 시작 실패를 숨기거나 무중단 배포로 기록하지 않는다.
- 앱 AAB는 이 서버 전용 보완으로 바뀌지 않는다. Play 승인 전 공개 정책은 43을 유지한다.
- 최종 운영 확인: API/DB/services 모두 정상, 서버 0.40.1, RTDN=true, 환불 조회 오류 0, 누락 복구 pending **2→0**, needsAttention=false. 테스트 알림을 수동 완료 처리하거나 지우지 않고 Google 조회 기반 처리로 해소했다. [최종 상태](reports/launch-sales-ads-20261124/service-health-final.json).

## 남은 운영 확인

1. Google 사전 검사와 심사 완료 후 44 및 스토어 이미지의 실제 게시 확인. 이후에만 서버 최신 버전 안내를 44로 갱신한다.
2. AdMob 앱 인증·준비 상태 재확인. 파일/스토어 링크가 맞는 현재 상태와 Google 승인을 구분한다.
3. 일반 사용자 기기의 실제 구매·복원·광고 송출과 수익, 실물 폰 성능. 이 기록의 테스트 주문을 실매출로 집계하지 않는다.

화면·정제된 결과는 [검증 자료](reports/launch-sales-ads-20261124/README.md), 설치 아이콘 프롬프트는 [아트 기록](../ArtSource/app-icon-v2-prompt.md)에 있다.
