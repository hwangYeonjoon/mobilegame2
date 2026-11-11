# 0.37 검증 보완과 BM 판단

버전 0.37.0 / Android code 40 / 전투 규칙 27을 유지한다. 이번 변경은 측정·Android 검증 보완이며 운영 서버나 Play에 배포하지 않는다. `region038`은 검증 실행 파일명이지 0.38 출시 표시가 아니다.

## 무료와 구매의 차이

**무료로 같은 성장 상한·콘텐츠에 도달하고, 구매하면 기다리는 시간을 줄여 먼저 강해진다**는 방향을 유지한다. 무료와 유료 보급량을 같게 맞추거나 무료 보상을 줄이지 않는다. 현재 상품 가격·구성·구매 제한은 유지한다.

[0.36 경제 측정](reports/review036.md)에서 보석을 보존하거나 의상에만 사용한 경로도 모두 60작전을 완료했다. 자연 조작에서 유료 경로가 같은 시드의 보존 경로보다 중앙값 3.5일, 활동 시간 84.5분 빨랐고 일부 시드에서는 오히려 늦었다. 이 결과는 하루 최대 6판·자동 강화·봇 조작을 가정한 것으로, 사람에게 “20% 빨라짐”을 보장하는 상품 설명으로 사용하면 안 된다.

18~24작전에서는 보급 1회로 무기 약 2단계의 추가 성장, 무료 반복 약 8~10분을 줄이는 효과가 있었다. 같은 장비에 도달한 무료/구매 경로의 결과가 같은 것은 의도에 맞는다. 남은 패배가 전부 실력 때문이라는 인과 증명은 아니다. 다른 성장 대상·빌드·조작도 영향을 준다.

이 정도 차이는 현재 설계에서 **유지하며 사람에게 확인할 범위**로 판단한다. 업계 평균이나 구매 만족도가 입증된 가격이라는 뜻은 아니다. 구매 전 순수 추가 성장 표시, 효과가 없을 때 판매 차단, 하루 구매 제한, 동일 조건 실력전의 고정 능력치를 유지한다. 성장 상한 이후 보급 수요가 사라지는 사실도 유지되며, 이를 숨기려고 상한이나 무료 반복 시간을 늘리지 않는다.

## 확대 측정

[작전별 전후 보고서](reports/region037-expanded.md). 지역당 1·3·4·6번째 작전으로 생존과 보스 목표를 포함한다. 조건당 10시드, 두 조작으로 각각 800판이다. 0.36 무료 보석 보존 경로에서 실제 획득한 첫 진입 장비를 사용한다. 장비가 없는 진입 경로를 임의 장비로 채우지 않는다.

`RegionalAudit`는 장비 복원 후 공격력과 체력을 원본에 대조한다. 요약 도구는 중복·누락 시드, 전후 장비 불일치, 피해 출처 합계 오류를 거절한다. 접촉·적 공격·지형·보스 피해를 나누고 보스 전 패배와 보스 도달 후 패배, 회복량, 저체력 시간을 기록한다. 지역 합계만 보고 생존/보스 목표 차이를 숨기지 않는다.

자연 조작 271/400 → 271/400, 회피 조작 369/400 → 367/400이다. 전체가 일괄 쉬워졌다는 이전 60판의 인상을 확대 표본에 일반화하지 않는다. 지역별 차이는 남는다.

- 폐철도: 자연 31/40 → 28/40, 회피 40/40 → 38/40. 자연 9·12작전과 회피 10작전의 감소를 관찰 대상으로 남긴다. 자연 피해 중앙값은 1,941 → 2,024다.
- 중앙 공장: 자연 17/40 → 13/40, 회피 32/40 → 30/40. 자연 25·27작전에서 각각 3승 감소, 28·30작전은 각각 1승 증가했다. 새 방패 소환과 타깃 분산을 사람 플레이에서 확인할 대상이다.
- 냉각 시설: 자연 11/40 → 19/40, 회피는 40/40 유지. 멈춰 방어하는 동안의 압박 감소·측면 약점과 맞는 방향이지만, 이 비교만으로 특정 행동 하나의 인과 효과를 분리한 것은 아니다.

작전·조작당 10시드이므로 작은 차이를 확정하지 않는다. 난이도를 일괄 올리거나 결과표의 목표 승률에 맞춰 즉시 수치를 다시 깎지 않았다. 사람에게는 이전보다 지역 적을 읽기 쉬워졌는지, 해당 지역에서 실패 원인을 이해하는지 확인한다. 기존 첫 진입 실패는 이후 무료 재도전·강화까지 포함한 달력의 완료 여부와 다른 지표다.

재생성:

```sh
python3 Tools/regional_review.py inputs docs/reports/review036-calendar.json.gz TestResults/region038-inputs.json
dotnet run --project Tools/CoreChecks -- --regional-audit TestResults/region038-inputs.json TestResults/region038-after.json
python3 Tools/regional_review.py report docs/reports/region037-expanded-inputs.json.gz docs/reports/region037-expanded-before.json.gz docs/reports/region037-expanded-after.json.gz docs/reports/region037-expanded.md
python3 -m unittest discover -s Tools -p test_regional_review.py
```

변경 전은 `3194f41`의 코어와 해당 테스트를 임시 폴더에 복원하고 **현재와 같은 RegionalAudit 측정기**를 사용한다. 새 버전으로 28일 경제 경로 전체를 다시 진행한 것은 아니다. 전투 수 1,600을 사람 표본 1,600명처럼 해석하지 않는다.

## Android 실행 재현

`Tools/start_android_qa_emulator.sh`의 전용 QA AVD를 사용한다. 로그인된 Play AVD는 변경하지 않는다. AVD 설정 파서는 키 주변 공백을 제거해 같은 설정이 두 번 생기지 않도록 했다. 부팅 완료(`sys.boot_completed=1`)와 설치 완료를 확인한 후 앱을 시작한다. Activity는 `com.unity3d.player.UnityPlayerGameActivity`다.

Mac의 QA 기본 그래픽 방식을 `host`로 변경했다. 이번 Apple M1에서 확인한 설정이다. 다른 환경에서 검은 화면이나 드라이버 문제가 생기면 `SCRAP_QA_GPU=swiftshader sh Tools/start_android_qa_emulator.sh`로 소프트웨어 방식에 돌아갈 수 있다. macOS의 OpenGL 브리지는 에뮬레이터에서 deprecated 경고가 나오므로 미래 OS·SDK에서도 같은 성능을 보장하지 않는다. 각 실행 전 이전 결과 폴더를 별도 보관하고 새 출력 폴더에서 시작해 중단된 실행이 과거 성공 보고서를 재사용하지 않도록 한다.

```sh
sh Tools/start_android_qa_emulator.sh
# 다른 터미널, SDK의 adb를 사용
adb -s emulator-5556 shell getprop sys.boot_completed
adb -s emulator-5556 install -r Builds/Android/ScrapGuard-qa.apk
adb -s emulator-5556 shell am start -n io.github.hwangyeonjoon.scrapguard.qa/com.unity3d.player.UnityPlayerGameActivity
```

`ProjectBuilder.BuildAndroidRegionalQA`는 `.qa` 패키지·새 오프라인 프로필로 실행한다. 기존 성장 화면 검사를 재사용해 보석 구매 확인, 실제 재료 차감과 강화, 재출격 능력치 적용을 검사한다. **Google 실결제나 운영 서버 네트워크 재시도 검사와는 다르다.**

이후 냉각 시설·심층 용광로·궤도 조립항에서 각각 60초 일반 스폰과 60초 밀집 장면을 실제 렌더링한다. 밀집 장면은 적 180명 미만일 때 210명까지 보충하고 스킬·체력을 고정하는 성능 부하 표본이다. 승률·밸런스 자료에 포함하지 않는다. 실제 벽시계 시간과 진행한 전투 시간을 함께 저장해 느린 에뮬레이터에서 진행이 늦어진 사실을 숨기지 않는다. 스크린샷 저장 시간은 프레임 표본 구간 밖에 둔다.

프레임 기록은 Unity의 실제 프레임 간격이며 `gfxinfo`만으로 Unity 화면 성능을 단정하지 않는다. 에뮬레이터에서 실행이 성공해도 실물 폰의 GPU·발열·터치 품질 합격을 뜻하지 않는다.

### 이번 실행 결과

[Android 실행 기록과 원본](reports/android037-runtime/README.md), [기계 검증 요약](reports/android037-runtime/verification.json).

- SwiftShader와 Apple M1 host 방식 모두 화면·구매/강화 검사 오류 0건, 세 지역 각 2분의 실제 렌더링을 완료했다.
- host 방식의 밀집 구간은 최대 240마리, 평균 54.7~57.7 FPS, 프레임 p95 16.7ms였다. p99 지연은 33.3~66.7ms로 남아 있어 고정 60 FPS나 저사양 실물 폰 합격으로 표시하지 않는다.
- Android IL2CPP 전투 입력 15개를 .NET으로 재생해 결과가 일치했다. 코어 검사 129개, 요약 도구 검사 4개도 통과했다.
- QA APK는 118,427,353 bytes, arm64·v2 서명·16KB ZIP/ELF 정렬을 통과했다. `apk.json`의 `device_execution: not tested`는 파일 검사 도구의 범위이며, 후속 실행 결과는 위 런타임 기록에서 확인한다.
- 검증 후 에뮬레이터에는 자동 실행 QA 대신 기존 수동 체험 APK를 다시 설치한다. 사람 테스트를 대신 수행한 것으로 기록하지 않는다.

## 사람이 확인할 남은 항목

기존 관찰표를 사용해 3~5명에게 직접 플레이하게 해야 한다. 냉각 방패의 측면 공략을 알아채는지, 지역이 바뀌면 새롭게 느끼는지, 보급 구매 제안을 도움과 압박 중 무엇으로 느끼는지 확인한다. 테스트 보석으로 확인하며 실제 결제를 요구하지 않는다. 사람 테스트·실물 폰 검증은 수행 전까지 계속 미실시로 표기한다.
