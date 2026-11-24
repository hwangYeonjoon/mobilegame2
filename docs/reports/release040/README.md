# 0.40 정식 출시 검증 기록

2026-11-23: 코어 137, 서버 435, Unity 178 검사 통과. AAB 업로드 해시 일치, 내부 트랙 게시, 프로덕션/Alpha Google 검토 중을 Console에서 확인했다.

- `release040-apk.json`: 정식 번들에서 생성한 서명 APK 패키지/16KB 검사. 이 파일의 device_execution은 패키지 검사 당시 값이다. 이후 설치·타이틀 실행까지 확인했다.
- `release040-server-live.png`: 운영 서버 전환 후 QA 에뮬레이터의 정식 APK 시작 화면.
- `release040-production-commerce-stage.json`: 별도 검증 DB에서 실제 Google 환불 API 조회 성공.
- `release040-production-health.json`: 전환 후 운영 건강 상태.
- `release040-production-smoke.json`: 실제 운영 API 신규 가입/복구/저장 재시도/실판매 목록 검사. 계정 비밀값은 포함하지 않는다.

실제 카드 청구, Play 서명 설치본의 Google 로그인·결제 종단 흐름, 실기기 성능은 이 검사로 증명하지 않는다. 이전 테스트 구매 계정은 운영 실결제가 제한된다.
