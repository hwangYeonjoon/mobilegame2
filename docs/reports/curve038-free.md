# 지역 몬스터 확대 회귀 측정

변경 전 build 40 / 규칙 27, 변경 후 build 41 / 규칙 28. 각각 800판, 총 1,600판.
10지역 × 지역당 진입·생존·중간 보스·최종 작전 4곳 × 두 조작 × 10시드. 보석 보존 경로의 실제 첫 진입 장비를 복원하고 공격력·체력을 대조했다.
동일 장비·동일 시드의 지역 행동 변경 비교이며, 새 규칙으로 28일 경제 경로 전체를 다시 진행한 자료는 아니다. 자연 조작은 사람의 평균 실력이 아니다.

| 지역 | 조작 | 승리 전→후 /40 | 승→패 / 패→승 | 피해 중앙값 전→후 | 저체력 시간 비율 중앙값 전→후 |
|---|---|---:|---:|---:|---:|
| 고철 정비소 | natural_legacy | 40→40 | 0 / 0 | 386→386 | 0.0%→0.0% |
| 고철 정비소 | controlled_avoid | 40→40 | 0 / 0 | 66→66 | 0.0%→0.0% |
| 폐철도 | natural_legacy | 28→28 | 0 / 0 | 2024→2024 | 4.2%→4.2% |
| 폐철도 | controlled_avoid | 38→38 | 0 / 0 | 808→808 | 0.0%→0.0% |
| 오염 처리장 | natural_legacy | 22→25 | 2 / 5 | 3326→3153 | 9.0%→3.1% |
| 오염 처리장 | controlled_avoid | 24→33 | 4 / 13 | 3257→2896 | 20.0%→12.3% |
| 냉각 시설 | natural_legacy | 19→19 | 0 / 0 | 3823→3823 | 12.7%→12.7% |
| 냉각 시설 | controlled_avoid | 40→40 | 0 / 0 | 783→783 | 0.0%→0.0% |
| 중앙 공장 | natural_legacy | 13→16 | 9 / 12 | 4336→4324 | 14.9%→15.4% |
| 중앙 공장 | controlled_avoid | 30→33 | 5 / 8 | 2426→1096 | 2.1%→0.0% |
| 고압 발전소 | natural_legacy | 33→33 | 0 / 0 | 3526→3526 | 1.2%→1.2% |
| 고압 발전소 | controlled_avoid | 40→40 | 0 / 0 | 1135→1135 | 0.0%→0.0% |
| 심층 용광로 | natural_legacy | 32→32 | 0 / 0 | 3891→4285 | 0.0%→0.0% |
| 심층 용광로 | controlled_avoid | 40→40 | 0 / 0 | 858→863 | 0.0%→0.0% |
| 자력 선별장 | natural_legacy | 28→26 | 2 / 0 | 4836→4878 | 2.8%→3.3% |
| 자력 선별장 | controlled_avoid | 40→40 | 0 / 0 | 1945→1967 | 0.0%→0.0% |
| 폭풍 송전탑 | natural_legacy | 33→31 | 2 / 0 | 5854→6209 | 1.3%→2.1% |
| 폭풍 송전탑 | controlled_avoid | 40→40 | 0 / 0 | 2693→2734 | 0.0%→0.0% |
| 궤도 조립항 | natural_legacy | 23→22 | 1 / 0 | 6889→6994 | 3.6%→3.9% |
| 궤도 조립항 | controlled_avoid | 35→34 | 1 / 0 | 3579→3690 | 0.0%→0.0% |

## 작전·목표별 결과

피해 비중은 변경 후 받은 총피해를 합산한 비율이며, 개별 판 비율의 중앙값이 아니다. 기타는 미분류와 조건 피해다. 패배는 보스 도달 여부로 구분한다.

| 작전 | 목표 | 조작 | 승리 전→후 /10 | 후 패배: 보스 전 / 보스 도달 | 후 피해: 접촉 / 적 공격 / 지형 / 보스 / 기타 % |
|---:|---|---|---:|---:|---|
| 1 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 36 / 22 / 0 / 42 / 0 |
| 1 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 75 / 0 / 0 / 25 / 0 |
| 3 | Survive | natural_legacy | 10→10 | 0 / 0 | 35 / 65 / 0 / 0 / 0 |
| 3 | Survive | controlled_avoid | 10→10 | 0 / 0 | 100 / 0 / 0 / 0 / 0 |
| 4 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 10 / 5 / 0 / 85 / 0 |
| 4 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 100 / 0 / 0 / 0 / 0 |
| 6 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 17 / 5 / 0 / 78 / 0 |
| 6 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 62 / 0 / 0 / 38 / 0 |
| 7 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 3 / 40 / 1 / 53 / 4 |
| 7 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 2 / 28 / 0 / 66 / 4 |
| 9 | Survive | natural_legacy | 6→6 | 4 / 0 | 1 / 87 / 1 / 0 / 11 |
| 9 | Survive | controlled_avoid | 10→10 | 0 / 0 | 19 / 67 / 2 / 0 / 12 |
| 10 | Eliminate | natural_legacy | 9→9 | 1 / 0 | 1 / 48 / 1 / 41 / 8 |
| 10 | Eliminate | controlled_avoid | 8→8 | 2 / 0 | 3 / 55 / 0 / 32 / 10 |
| 12 | Eliminate | natural_legacy | 3→3 | 3 / 4 | 1 / 59 / 1 / 28 / 10 |
| 12 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 5 / 35 / 1 / 58 / 1 |
| 13 | Eliminate | natural_legacy | 7→9 | 0 / 1 | 0 / 21 / 24 / 53 / 2 |
| 13 | Eliminate | controlled_avoid | 8→9 | 1 / 0 | 4 / 68 / 3 / 13 / 12 |
| 15 | Survive | natural_legacy | 6→7 | 3 / 0 | 1 / 66 / 24 / 0 / 9 |
| 15 | Survive | controlled_avoid | 6→8 | 2 / 0 | 3 / 87 / 0 / 0 / 10 |
| 16 | Eliminate | natural_legacy | 5→5 | 3 / 2 | 0 / 44 / 24 / 26 / 6 |
| 16 | Eliminate | controlled_avoid | 5→8 | 2 / 0 | 3 / 81 / 1 / 4 / 10 |
| 18 | Eliminate | natural_legacy | 4→4 | 3 / 3 | 1 / 40 / 20 / 30 / 8 |
| 18 | Eliminate | controlled_avoid | 5→8 | 2 / 0 | 2 / 83 / 0 / 5 / 10 |
| 19 | Eliminate | natural_legacy | 4→4 | 2 / 4 | 3 / 45 / 0 / 44 / 8 |
| 19 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 16 / 12 / 0 / 71 / 2 |
| 21 | Survive | natural_legacy | 7→7 | 3 / 0 | 8 / 81 / 0 / 0 / 11 |
| 21 | Survive | controlled_avoid | 10→10 | 0 / 0 | 31 / 58 / 0 / 0 / 11 |
| 22 | Eliminate | natural_legacy | 5→5 | 4 / 1 | 5 / 67 / 0 / 23 / 5 |
| 22 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 32 / 13 / 0 / 55 / 0 |
| 24 | Eliminate | natural_legacy | 3→3 | 4 / 3 | 5 / 62 / 0 / 26 / 6 |
| 24 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 19 / 29 / 0 / 48 / 5 |
| 25 | Eliminate | natural_legacy | 3→3 | 0 / 7 | 2 / 41 / 1 / 51 / 4 |
| 25 | Eliminate | controlled_avoid | 8→10 | 0 / 0 | 15 / 34 / 0 / 48 / 4 |
| 27 | Survive | natural_legacy | 4→7 | 3 / 0 | 5 / 80 / 2 / 0 / 13 |
| 27 | Survive | controlled_avoid | 6→8 | 2 / 0 | 12 / 75 / 0 / 0 / 13 |
| 28 | Eliminate | natural_legacy | 4→3 | 5 / 2 | 3 / 67 / 1 / 20 / 8 |
| 28 | Eliminate | controlled_avoid | 8→8 | 1 / 1 | 16 / 58 / 0 / 15 / 11 |
| 30 | Eliminate | natural_legacy | 2→3 | 3 / 4 | 4 / 59 / 0 / 32 / 5 |
| 30 | Eliminate | controlled_avoid | 8→7 | 3 / 0 | 10 / 58 / 0 / 18 / 14 |
| 31 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 10 / 17 / 0 / 71 / 2 |
| 31 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 44 / 1 / 0 / 54 / 1 |
| 33 | Survive | natural_legacy | 8→8 | 2 / 0 | 15 / 75 / 2 / 0 / 8 |
| 33 | Survive | controlled_avoid | 10→10 | 0 / 0 | 95 / 3 / 0 / 0 / 2 |
| 34 | Eliminate | natural_legacy | 9→9 | 1 / 0 | 7 / 46 / 2 / 41 / 4 |
| 34 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 58 / 3 / 0 / 39 / 0 |
| 36 | Eliminate | natural_legacy | 6→6 | 4 / 0 | 9 / 53 / 1 / 32 / 6 |
| 36 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 57 / 7 / 1 / 33 / 2 |
| 37 | Eliminate | natural_legacy | 8→8 | 0 / 2 | 15 / 13 / 0 / 68 / 4 |
| 37 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 59 / 7 / 0 / 33 / 1 |
| 39 | Survive | natural_legacy | 10→10 | 0 / 0 | 33 / 40 / 1 / 0 / 26 |
| 39 | Survive | controlled_avoid | 10→10 | 0 / 0 | 98 / 0 / 0 / 0 / 2 |
| 40 | Eliminate | natural_legacy | 7→7 | 1 / 2 | 24 / 26 / 0 / 40 / 10 |
| 40 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 57 / 7 / 1 / 35 / 0 |
| 42 | Eliminate | natural_legacy | 7→7 | 0 / 3 | 17 / 11 / 0 / 65 / 7 |
| 42 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 57 / 0 / 1 / 37 / 6 |
| 43 | Eliminate | natural_legacy | 8→8 | 0 / 2 | 6 / 24 / 0 / 68 / 1 |
| 43 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 43 / 1 / 0 / 55 / 0 |
| 45 | Survive | natural_legacy | 8→8 | 2 / 0 | 16 / 74 / 0 / 0 / 9 |
| 45 | Survive | controlled_avoid | 10→10 | 0 / 0 | 77 / 21 / 0 / 0 / 1 |
| 46 | Eliminate | natural_legacy | 8→8 | 1 / 1 | 11 / 44 / 0 / 40 / 5 |
| 46 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 60 / 17 / 0 / 21 / 2 |
| 48 | Eliminate | natural_legacy | 4→2 | 4 / 4 | 13 / 45 / 0 / 38 / 4 |
| 48 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 37 / 19 / 0 / 41 / 3 |
| 49 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 21 / 28 / 0 / 48 / 3 |
| 49 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 73 / 7 / 0 / 19 / 0 |
| 51 | Survive | natural_legacy | 10→10 | 0 / 0 | 26 / 67 / 0 / 0 / 7 |
| 51 | Survive | controlled_avoid | 10→10 | 0 / 0 | 92 / 6 / 0 / 0 / 1 |
| 52 | Eliminate | natural_legacy | 7→7 | 2 / 1 | 22 / 45 / 0 / 30 / 3 |
| 52 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 78 / 11 / 0 / 10 / 2 |
| 54 | Eliminate | natural_legacy | 6→4 | 0 / 6 | 15 / 36 / 0 / 43 / 5 |
| 54 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 67 / 11 / 0 / 22 / 0 |
| 55 | Eliminate | natural_legacy | 7→7 | 2 / 1 | 29 / 34 / 0 / 32 / 5 |
| 55 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 68 / 20 / 0 / 9 / 3 |
| 57 | Survive | natural_legacy | 8→8 | 2 / 0 | 39 / 55 / 0 / 0 / 5 |
| 57 | Survive | controlled_avoid | 8→8 | 2 / 0 | 71 / 25 / 0 / 0 / 4 |
| 58 | Eliminate | natural_legacy | 6→6 | 2 / 2 | 32 / 35 / 0 / 30 / 3 |
| 58 | Eliminate | controlled_avoid | 9→9 | 1 / 0 | 57 / 32 / 0 / 5 / 6 |
| 60 | Eliminate | natural_legacy | 2→1 | 4 / 5 | 30 / 41 / 1 / 24 / 4 |
| 60 | Eliminate | controlled_avoid | 8→7 | 2 / 1 | 54 / 32 / 0 / 10 / 4 |

## 해석 범위

- 각 작전·조작은 10시드다. 1~2승 차이를 확정적인 난이도 변화로 해석하지 않는다. 지역 합계도 서로 다른 목표를 섞으므로 상세 표와 함께 읽는다.
- 예고 중 멈추는 적은 추격 압박이 줄 수 있다. 전체 승률을 일정하게 만들려고 모든 적 체력·공격력을 일괄 올리지 않는다.
- 고정 Lv.5 장비, 인위적으로 주입한 밀집 화면, 무료/유료 달력 결과는 별도 자료다. 이 표를 사람의 승률·구매 전환·실물 폰 성능으로 사용하지 않는다.

curve038-free-inputs.json.gz SHA-256: `1305d48e62f4c4cea0cc3d29fd20b276b658d2523c849202ac9f7ff2f77598b8`

region037-expanded-after.json.gz SHA-256: `6d446d4ae551c16e5ca1b4153257ff56619c365e6105754158edd5c8a2f09bfa`

curve038-free-after.json.gz SHA-256: `64a1ab8e90b456105ba583dd393557bbeb30ab582086a4d43599915a6ae19c3c`
