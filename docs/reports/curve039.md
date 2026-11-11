# 0.39 중반·일반 최종 작전 비교

build 41 / 규칙 28 → build 42 / 규칙 29. 각 360판, 작전·조작별 10시드.
기존 달력에서 실제 얻은 첫 진입 장비를 고정한 비교다. 새 경제 여정이나 사람의 평균 실력을 측정한 것은 아니다. 자연 조작은 회피가 서툰 조건을 점검하는 봇이다.

| 지역 | 조작 | 승리 전→후 /40 | 저체력 시간 비율 중앙값 전→후 |
|---|---|---:|---:|
| 냉각 시설 | natural_legacy | 19→25 | 12.7%→13.6% |
| 냉각 시설 | controlled_avoid | 40→40 | 0.0%→0.0% |
| 중앙 공장 | natural_legacy | 16→25 | 15.4%→15.0% |
| 중앙 공장 | controlled_avoid | 33→34 | 0.0%→0.0% |
| 궤도 조립항 | natural_legacy | 22→28 | 3.9%→2.5% |
| 궤도 조립항 | controlled_avoid | 34→35 | 0.0%→0.0% |

| 작전 | 조작 | 승리 전→후 /10 | 승→패 / 패→승 | 변경 후 패배: 보스 전 / 도달 후 |
|---:|---|---:|---:|---:|
| 18 | natural_legacy | 4→4 | 0 / 0 | 3 / 3 |
| 18 | controlled_avoid | 8→8 | 0 / 0 | 2 / 0 |
| 19 | natural_legacy | 4→8 | 0 / 4 | 1 / 1 |
| 19 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 21 | natural_legacy | 7→5 | 3 / 1 | 5 / 0 |
| 21 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 22 | natural_legacy | 5→6 | 0 / 1 | 4 / 0 |
| 22 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 24 | natural_legacy | 3→6 | 2 / 5 | 4 / 0 |
| 24 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 25 | natural_legacy | 3→8 | 0 / 5 | 0 / 2 |
| 25 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 27 | natural_legacy | 7→7 | 0 / 0 | 3 / 0 |
| 27 | controlled_avoid | 8→8 | 0 / 0 | 2 / 0 |
| 28 | natural_legacy | 3→4 | 0 / 1 | 5 / 1 |
| 28 | controlled_avoid | 8→9 | 0 / 1 | 1 / 0 |
| 30 | natural_legacy | 3→6 | 0 / 3 | 3 / 1 |
| 30 | controlled_avoid | 7→7 | 0 / 0 | 3 / 0 |
| 31 | natural_legacy | 10→10 | 0 / 0 | 0 / 0 |
| 31 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 36 | natural_legacy | 6→6 | 0 / 0 | 4 / 0 |
| 36 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 42 | natural_legacy | 7→7 | 0 / 0 | 0 / 3 |
| 42 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 48 | natural_legacy | 2→2 | 0 / 0 | 4 / 4 |
| 48 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 54 | natural_legacy | 4→4 | 0 / 0 | 0 / 6 |
| 54 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 55 | natural_legacy | 7→7 | 0 / 0 | 2 / 1 |
| 55 | controlled_avoid | 10→10 | 0 / 0 | 0 / 0 |
| 57 | natural_legacy | 8→8 | 0 / 0 | 2 / 0 |
| 57 | controlled_avoid | 8→8 | 0 / 0 | 2 / 0 |
| 58 | natural_legacy | 6→6 | 0 / 0 | 1 / 3 |
| 58 | controlled_avoid | 9→9 | 0 / 0 | 1 / 0 |
| 60 | natural_legacy | 1→7 | 0 / 6 | 3 / 0 |
| 60 | controlled_avoid | 7→8 | 0 / 1 | 2 / 0 |

변경 대상 밖 18·31·36·42·48·54작전 120판은 승패뿐 아니라 저장한 모든 전투 수치가 동일하다.
냉각 시설의 자연 조작 저체력 비율은 감소하지 않았다. 승리 증가를 모든 체감 문제의 해결로 해석하지 않는다. 일반 60작전을 3~5승이라는 특정 숫자에 맞추기 위한 추가 조정은 하지 않았다.

## 0.38 생존 패배 시점

각 조건 10판 중 패배만 집계한다. 소수 표본의 분포이며 사람의 사망 시점으로 일반화하지 않는다. 기본 연습을 일괄 후반으로 옮기는 대신 각 계정의 실제 실패 시간대를 별도로 제공한다.

| 작전 | 조작 | 패배 수 | 사망 초 최소 / 중앙 / 최대 |
|---:|---|---:|---:|
| 15 | natural_legacy | 3 | 178.3 / 217.9 / 230.8 |
| 15 | controlled_avoid | 2 | 114.1 / 130.1 / 146.2 |
| 21 | natural_legacy | 3 | 145.8 / 169.4 / 213.4 |
| 21 | controlled_avoid | 0 | 해당 없음 |
| 27 | natural_legacy | 3 | 176.3 / 186.6 / 219.9 |
| 27 | controlled_avoid | 2 | 176.1 / 177.2 / 178.3 |
