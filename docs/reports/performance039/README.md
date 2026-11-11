# 궤도 지역 성능 저하 재확인

[동일 부하 비교표](comparison.md), [버전·측정기·APK 해시](provenance.json), 원본 `0.37/`·`0.38/`.

각 버전 일반/밀집 3회씩, 총 12개 구간을 Android IL2CPP에서 실행했다. 모두 오류 0건이며 대응 표본의 전투 진행량·처치·최대 적·진단 해시가 일치한다. M1 host OpenGL, API35 전용 QA AVD, 540×960, 같은 장비·시드·간결 효과다. 실행 중 별도 Unity 빌드와 대규모 .NET 측정은 돌리지 않았다.

밀집 처리량 중앙값은 54.6→57.2 프레임/초, 일반 구간은 52.7→51.6이다. 밀집 0.38의 반복 간 범위도 49.5~58.1로 크다. **기존 한 번의 57.4→49.4 차이가 일관된 0.38 코드 회귀라는 증거는 얻지 못했다.** 동등성이나 성능 개선이 입증됐다는 뜻도 아니다. 이번 비교를 근거로 전투 규칙이나 렌더링 코드를 임의로 바꾸지 않았다.

프레임 p99 33~83ms 지연은 남아 있다. 호출 범위의 장면 갱신 CPU 시간은 코어 계산보다 긴 표본이 많지만, 지연된 Canvas/GPU 작업 전체를 포함하지 않으므로 근본 원인을 확정하지 않는다. 실물 중저가 Android의 FPS·발열·터치 품질은 여전히 별도 확인이 필요하다.

## 재현

1. `499814b`와 `618df7a`의 `Assets`, `Packages`, `ProjectSettings`를 각각 별도 임시 프로젝트에 추출한다. 작업 저장소의 커밋이나 브랜치를 되돌리지 않는다.
2. `provenance.json`에 적은 4개 측정 진입 파일만 이 변경에서 복사한다. 나머지 전투/렌더링 코드는 각 원본 버전을 유지한다.
3. Unity 6000.3.7f1에서 `ScrapGuard.Editor.ProjectBuilder.BuildAndroidPerformanceQA`를 **순차** 실행한다. 동시에 실행하면 공용 Gradle 데몬 중단이 다른 빌드를 끊을 수 있다.
4. 같은 QA AVD에 각 `.qa` APK를 차례로 설치한다. 실행 전 기존 `PerformanceCompare` 폴더를 다른 이름으로 보존한다. 신규 폴더에 결과가 저장되고 앱이 종료될 때까지 기다린다.
5. 결과 폴더를 새 로컬 경로로 가져온 뒤 아래 도구로 6개씩의 완전한 대응 표본인지 검사한다.

```sh
python3 Tools/performance_pair_review.py docs/reports/performance039/0.37 docs/reports/performance039/0.38 TestResults/performance039-reproduced.md
```

한 프레임마다 고정 틱 1회만 진행하는 비교 전용 실험이다. 실제 게임의 시간 누적 방식 FPS와 혼용하지 않는다. 순서는 0.37 다음 0.38이며, 순서 교대·VFX 위치·OS/GPU 스케줄링까지 통제하지는 않았다.
