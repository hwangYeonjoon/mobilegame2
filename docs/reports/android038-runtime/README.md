# Android 0.38 실행 기록

0.38.0 / build 41 / 전투 규칙 28. 전용 `ScrapGuard_QA_API35`, `emulator-5556`, Apple M1 host OpenGL ES. **실물 폰·사람 테스트가 아니다.**

`ProjectBuilder.BuildAndroidRegionalQA`로 만든 `.qa` 패키지를 설치했다. 기존 0.37 결과 폴더를 별도 보관한 뒤 새 폴더에서 시작했다. 운영 앱/서버/Play 트랙은 변경하지 않았다.

## 화면과 연습

- 구매 확인 → 보급 구매·즉시 강화 → 해당 능력치로 재출격의 기존 검사를 포함한다. 오프라인 테스트 보석이며 실제 Google 결제·운영 네트워크 재시도 검사가 아니다.
- [연습 선택](15-practice-selector.png), [패배 화면의 무료 연습](45-survival-loss.png), [최근 피해 출처](46-recent-damage.png)를 직접 확인했다. 겹침이나 잘린 버튼은 없었다.
- 패배 화면은 체력을 1로 설정하고 적 독성 폭발을 주입한 UI 전용 표본이다. 이 화면의 1 피해·40초·보상 수치는 밸런스 결과가 아니다.
- 해당 패배 화면의 실제 버튼으로 15작전 생존 연습에 들어간 뒤 27·57작전도 연습했다. 시작 이후 전투에 체력/적을 주입하지 않고 입력을 기록했다. QA 프로필은 성장한 장비이므로 신규 이용자의 성공률로 해석하지 않는다.
- 연습 종료 시 골드·부품·보석·총 작전 수·현장 보급 시간이 늘지 않는지 검사한다. 서버의 중복·보상 차단 검사는 별도 서버 테스트에 포함한다.

## 실행 및 부하 자료의 범위

냉각(19)·용광로(37)·궤도(55)의 일반 스폰 60초와 밀집 60초를 실제 Android 화면으로 렌더링한다. 밀집 구간은 적 수·체력·스킬을 주입하는 부하 표본이다. 그 장면의 처치·승률을 일반 전투 밸런스로 사용하지 않는다.

`frames-*.json`은 실제 프레임 간격과 진행한 전투 시간이다. 평균 FPS는 frames/wallSeconds로 계산한다. 프레임 p99 지연을 포함하며, Mac host GPU 결과를 저사양 갤럭시 성능이나 발열·터치 품질로 바꾸어 말하지 않는다. 부하 구간 시작 전에 대규모 .NET 측정과 Unity 빌드를 마쳤다.

`device-status.json`의 세 번 수집 모두 앱 실행 중이며, 상태는 `emulator_only`다. Unity GL 프레임은 gfxinfo만으로 판정하지 않는다. `apk.json`과 `playtest-apk.json`의 실행 미검증 표시는 APK 파일 검사 도구 자체의 범위다.

## 재현

```sh
# 전용 QA AVD 사용. 로그인한 Play AVD를 초기화하지 않는다.
sh Tools/start_android_qa_emulator.sh
# SDK adb로 QA APK 설치 후 실행
adb -s emulator-5556 install -r Builds/Android/ScrapGuard-qa.apk
adb -s emulator-5556 shell am start -n io.github.hwangyeonjoon.scrapguard.qa/com.unity3d.player.UnityPlayerGameActivity
# 실행이 끝나면 RegionalRuntime 폴더를 새 로컬 폴더에 가져와 확인한다.
python3 Tools/verify_runtime_replays.py docs/reports/android038-runtime --output TestResults/android038-reproduced.json
```

재생 도구를 사용하기 전에 현재 `Tools/CoreChecks`를 빌드한다. 자동 QA와 직접 플레이용 APK는 같은 `.qa` 패키지를 사용하므로 자동 검증 뒤 수동 체험 APK로 교체한다. 참가자에게 자동 QA APK를 전달하지 않는다. 수동 빌드는 현장 보급 안내의 제외 대상을 “모든 연습”으로 정리한 문구 변경을 포함한다.

## 완료 결과

화면·구매/강화·재출격·생존 연습·부하 검사 오류 **0건**. 생존 연습 15·27·57작전이 모두 60초 완료됐으며 성장 보상 누수가 없었다.

| 작전 | 구간 | 평균 FPS | 프레임 p95 / p99 (ms) | 최대 적 | Unity 메모리 (MiB) |
|---:|---|---:|---:|---:|---:|
| 19 | crowded | 57.2 | 16.7 / 33.3 | 236 | 89.5 |
| 19 | opening | 58.7 | 16.7 / 33.3 | 59 | 86.6 |
| 37 | crowded | 56.1 | 16.7 / 33.3 | 240 | 89.6 |
| 37 | opening | 58.9 | 16.7 / 33.3 | 146 | 87.6 |
| 55 | crowded | 49.4 | 33.3 / 83.3 | 240 | 90.4 |
| 55 | opening | 45.3 | 33.3 / 150.0 | 240 | 90.2 |

[냉각 밀집](runtime-19-crowded.png), [용광로 밀집](runtime-37-crowded.png), [궤도 밀집](runtime-55-crowded.png), [생존 연습 시작](47-survival-start-14.png), [실제 연습 결과](48-survival-result-14.png). 프레임 지연은 남아 있으며 고정 60 FPS 합격으로 표시하지 않는다.

Android IL2CPP 입력 18개를 .NET으로 재생해 처치·합체 횟수·체력이 모두 일치했다. 기존 16개 검사 후 최종 폴더의 파일 내용이 동일함을 확인하고 추가 생존 연습 2개만 검사했다. [결과](replays.json), [검사 수와 범위](checks.json). 화면 캡처는 73개이며 주요 화면만 이 폴더에 보존한다.

궤도 구간의 평균과 p99는 냉각·용광로보다 나쁘다. 원인을 코드 또는 에뮬레이터 부하로 확정하지 않았고, 실제 폰의 성능 합격이나 최적화 완료로 취급하지 않는다.
