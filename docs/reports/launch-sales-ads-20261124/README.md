# 출시 판매·광고 검증 자료

전체 범위와 제한은 [운영 기록](../../28-launch-sales-ads-041.md)을 따른다.

| 자료 | 확인한 내용 |
|---|---|
| `google-test-order.png`, `google-test-skip-order.png` | Google 테스트 결제수단·비청구 주문 표시. 각각 보석 300, 영구 광고 스킵 |
| `launch-google-test-grants.json` | 두 테스트 주문의 서버 지급 결과 |
| `launch-google-skip-granted.json`, `launch-google-restored.json` | 광고 스킵 보상 지급 후 재시작·복원에도 값 동일 |
| `ad-skip-owned.png` | 광고 스킵 보유 화면 |
| `rewarded-test-ad.png` | code 44에서 실제 Google SDK의 Test Ad 표시 |
| `rewarded-ssv-granted.png` | SSV 지급 뒤 골드 2,400·부품 38·일일 1/2회 |
| `android44-runtime.json`, `android44-start.png` | 실제 Android 에뮬레이터 설치·실행·광고 지급 관찰 |
| `launch044-apk.json` | 서명·매니페스트·16KB 정렬 검사. 이 도구 자체의 device_execution은 not tested이며 별도 runtime 기록과 구분 |
| `packed-launcher-icon.png` | 실제 APK에서 추출한 새 설치 아이콘 |
| `launch-products-audit.json` | Play API 5종 판매 옵션·한국 가격 확인 |
| `launch-sales-ads-live.json` | 운영 판매·광고 설정 및 기존 43 공개 버전 정책 확인 |
| `service-health-final.json` | 운영 서버 0.40.1, RTDN 정상, 미처리 환불 알림 0건 |
| `server-checks.txt`, `core-checks.txt` | 서버 452개, 코어 137개 통과 |

실제 카드 청구, 광고 매출, AdMob 앱 인증 완료, 44 공개 완료의 증거는 아니다. 구매 토큰·계정 복구 코드·비공개 배포 환경은 포함하지 않는다. SDK 테스트 광고 화면과 서버 production 광고 모드 값은 서로 다른 개념이다.
