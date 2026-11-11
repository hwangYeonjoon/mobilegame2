# 지역 몬스터 확대 회귀 측정

변경 전 build 39 / 규칙 26, 변경 후 build 40 / 규칙 27. 각각 800판, 총 1,600판.
10지역 × 지역당 진입·생존·중간 보스·최종 작전 4곳 × 두 조작 × 10시드. 보석 보존 경로의 실제 첫 진입 장비를 복원하고 공격력·체력을 대조했다.
동일 장비·동일 시드의 지역 행동 변경 비교이며, 새 규칙으로 28일 경제 경로 전체를 다시 진행한 자료는 아니다. 자연 조작은 사람의 평균 실력이 아니다.

| 지역 | 조작 | 승리 전→후 /40 | 승→패 / 패→승 | 피해 중앙값 전→후 | 저체력 시간 비율 중앙값 전→후 |
|---|---|---:|---:|---:|---:|
| 고철 정비소 | natural_legacy | 40→40 | 0 / 0 | 335→386 | 0.0%→0.0% |
| 고철 정비소 | controlled_avoid | 40→40 | 0 / 0 | 0→66 | 0.0%→0.0% |
| 폐철도 | natural_legacy | 31→28 | 8 / 5 | 1941→2024 | 2.5%→4.2% |
| 폐철도 | controlled_avoid | 40→38 | 2 / 0 | 780→808 | 0.0%→0.0% |
| 오염 처리장 | natural_legacy | 22→22 | 0 / 0 | 3423→3326 | 11.8%→9.0% |
| 오염 처리장 | controlled_avoid | 22→24 | 3 / 5 | 3282→3257 | 15.7%→20.0% |
| 냉각 시설 | natural_legacy | 11→19 | 3 / 11 | 4011→3823 | 9.9%→12.7% |
| 냉각 시설 | controlled_avoid | 40→40 | 0 / 0 | 672→783 | 0.0%→0.0% |
| 중앙 공장 | natural_legacy | 17→13 | 7 / 3 | 4601→4336 | 14.6%→14.9% |
| 중앙 공장 | controlled_avoid | 32→30 | 7 / 5 | 1843→2426 | 0.0%→2.1% |
| 고압 발전소 | natural_legacy | 32→33 | 0 / 1 | 3784→3526 | 1.5%→1.2% |
| 고압 발전소 | controlled_avoid | 40→40 | 0 / 0 | 1038→1135 | 0.0%→0.0% |
| 심층 용광로 | natural_legacy | 34→32 | 4 / 2 | 4155→3891 | 0.4%→0.0% |
| 심층 용광로 | controlled_avoid | 40→40 | 0 / 0 | 810→858 | 0.0%→0.0% |
| 자력 선별장 | natural_legacy | 28→28 | 8 / 8 | 4705→4836 | 3.1%→2.8% |
| 자력 선별장 | controlled_avoid | 39→40 | 0 / 1 | 2112→1945 | 0.0%→0.0% |
| 폭풍 송전탑 | natural_legacy | 33→33 | 0 / 0 | 5858→5854 | 1.3%→1.3% |
| 폭풍 송전탑 | controlled_avoid | 40→40 | 0 / 0 | 1850→2693 | 0.0%→0.0% |
| 궤도 조립항 | natural_legacy | 23→23 | 11 / 11 | 7366→6889 | 5.0%→3.6% |
| 궤도 조립항 | controlled_avoid | 36→35 | 3 / 2 | 4098→3579 | 0.0%→0.0% |

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
| 7 | Eliminate | natural_legacy | 9→10 | 0 / 0 | 3 / 40 / 1 / 53 / 4 |
| 7 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 2 / 28 / 0 / 66 / 4 |
| 9 | Survive | natural_legacy | 8→6 | 4 / 0 | 1 / 87 / 1 / 0 / 11 |
| 9 | Survive | controlled_avoid | 10→10 | 0 / 0 | 19 / 67 / 2 / 0 / 12 |
| 10 | Eliminate | natural_legacy | 8→9 | 1 / 0 | 1 / 48 / 1 / 41 / 8 |
| 10 | Eliminate | controlled_avoid | 10→8 | 2 / 0 | 3 / 55 / 0 / 32 / 10 |
| 12 | Eliminate | natural_legacy | 6→3 | 3 / 4 | 1 / 59 / 1 / 28 / 10 |
| 12 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 5 / 35 / 1 / 58 / 1 |
| 13 | Eliminate | natural_legacy | 7→7 | 0 / 3 | 1 / 31 / 19 / 47 / 2 |
| 13 | Eliminate | controlled_avoid | 8→8 | 2 / 0 | 2 / 75 / 2 / 12 / 9 |
| 15 | Survive | natural_legacy | 6→6 | 4 / 0 | 2 / 71 / 18 / 0 / 9 |
| 15 | Survive | controlled_avoid | 3→6 | 4 / 0 | 4 / 89 / 0 / 0 / 8 |
| 16 | Eliminate | natural_legacy | 5→5 | 5 / 0 | 1 / 57 / 21 / 14 / 8 |
| 16 | Eliminate | controlled_avoid | 5→5 | 4 / 1 | 2 / 85 / 1 / 3 / 9 |
| 18 | Eliminate | natural_legacy | 4→4 | 4 / 2 | 1 / 45 / 20 / 28 / 5 |
| 18 | Eliminate | controlled_avoid | 6→5 | 4 / 1 | 2 / 84 / 0 / 6 / 8 |
| 19 | Eliminate | natural_legacy | 2→4 | 2 / 4 | 3 / 45 / 0 / 44 / 8 |
| 19 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 16 / 12 / 0 / 71 / 2 |
| 21 | Survive | natural_legacy | 5→7 | 3 / 0 | 8 / 81 / 0 / 0 / 11 |
| 21 | Survive | controlled_avoid | 10→10 | 0 / 0 | 31 / 58 / 0 / 0 / 11 |
| 22 | Eliminate | natural_legacy | 4→5 | 4 / 1 | 5 / 67 / 0 / 23 / 5 |
| 22 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 32 / 13 / 0 / 55 / 0 |
| 24 | Eliminate | natural_legacy | 0→3 | 4 / 3 | 5 / 62 / 0 / 26 / 6 |
| 24 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 19 / 29 / 0 / 48 / 5 |
| 25 | Eliminate | natural_legacy | 6→3 | 2 / 5 | 2 / 48 / 1 / 44 / 5 |
| 25 | Eliminate | controlled_avoid | 9→8 | 2 / 0 | 15 / 60 / 0 / 18 / 6 |
| 27 | Survive | natural_legacy | 7→4 | 6 / 0 | 3 / 88 / 1 / 0 / 9 |
| 27 | Survive | controlled_avoid | 7→6 | 4 / 0 | 6 / 85 / 0 / 0 / 8 |
| 28 | Eliminate | natural_legacy | 3→4 | 4 / 2 | 2 / 70 / 1 / 21 / 6 |
| 28 | Eliminate | controlled_avoid | 9→8 | 2 / 0 | 18 / 71 / 0 / 4 / 8 |
| 30 | Eliminate | natural_legacy | 1→2 | 6 / 2 | 5 / 71 / 1 / 18 / 6 |
| 30 | Eliminate | controlled_avoid | 7→8 | 2 / 0 | 6 / 64 / 0 / 21 / 9 |
| 31 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 10 / 17 / 0 / 71 / 2 |
| 31 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 44 / 1 / 0 / 54 / 1 |
| 33 | Survive | natural_legacy | 7→8 | 2 / 0 | 15 / 75 / 2 / 0 / 8 |
| 33 | Survive | controlled_avoid | 10→10 | 0 / 0 | 95 / 3 / 0 / 0 / 2 |
| 34 | Eliminate | natural_legacy | 9→9 | 1 / 0 | 7 / 46 / 2 / 41 / 4 |
| 34 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 58 / 3 / 0 / 39 / 0 |
| 36 | Eliminate | natural_legacy | 6→6 | 4 / 0 | 9 / 56 / 1 / 29 / 6 |
| 36 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 60 / 8 / 1 / 29 / 2 |
| 37 | Eliminate | natural_legacy | 7→8 | 0 / 2 | 15 / 13 / 0 / 68 / 4 |
| 37 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 59 / 7 / 0 / 33 / 1 |
| 39 | Survive | natural_legacy | 10→10 | 0 / 0 | 33 / 40 / 1 / 0 / 26 |
| 39 | Survive | controlled_avoid | 10→10 | 0 / 0 | 98 / 0 / 0 / 0 / 2 |
| 40 | Eliminate | natural_legacy | 9→7 | 1 / 2 | 24 / 26 / 0 / 40 / 10 |
| 40 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 57 / 7 / 1 / 35 / 0 |
| 42 | Eliminate | natural_legacy | 8→7 | 0 / 3 | 18 / 13 / 0 / 61 / 8 |
| 42 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 60 / 0 / 1 / 33 / 6 |
| 43 | Eliminate | natural_legacy | 7→8 | 0 / 2 | 6 / 24 / 0 / 68 / 1 |
| 43 | Eliminate | controlled_avoid | 9→10 | 0 / 0 | 43 / 1 / 0 / 55 / 0 |
| 45 | Survive | natural_legacy | 8→8 | 2 / 0 | 16 / 74 / 0 / 0 / 9 |
| 45 | Survive | controlled_avoid | 10→10 | 0 / 0 | 77 / 21 / 0 / 0 / 1 |
| 46 | Eliminate | natural_legacy | 8→8 | 1 / 1 | 11 / 44 / 0 / 40 / 5 |
| 46 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 60 / 17 / 0 / 21 / 2 |
| 48 | Eliminate | natural_legacy | 5→4 | 4 / 2 | 14 / 47 / 0 / 35 / 4 |
| 48 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 41 / 21 / 0 / 35 / 3 |
| 49 | Eliminate | natural_legacy | 10→10 | 0 / 0 | 21 / 28 / 0 / 48 / 3 |
| 49 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 73 / 7 / 0 / 19 / 0 |
| 51 | Survive | natural_legacy | 10→10 | 0 / 0 | 26 / 67 / 0 / 0 / 7 |
| 51 | Survive | controlled_avoid | 10→10 | 0 / 0 | 92 / 6 / 0 / 0 / 1 |
| 52 | Eliminate | natural_legacy | 7→7 | 2 / 1 | 22 / 45 / 0 / 30 / 3 |
| 52 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 78 / 11 / 0 / 10 / 2 |
| 54 | Eliminate | natural_legacy | 6→6 | 0 / 4 | 16 / 39 / 1 / 39 / 5 |
| 54 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 69 / 11 / 0 / 19 / 0 |
| 55 | Eliminate | natural_legacy | 6→7 | 2 / 1 | 30 / 35 / 0 / 29 / 5 |
| 55 | Eliminate | controlled_avoid | 10→10 | 0 / 0 | 69 / 21 / 0 / 8 / 3 |
| 57 | Survive | natural_legacy | 6→8 | 2 / 0 | 39 / 55 / 0 / 0 / 5 |
| 57 | Survive | controlled_avoid | 9→8 | 2 / 0 | 71 / 25 / 0 / 0 / 4 |
| 58 | Eliminate | natural_legacy | 5→6 | 2 / 2 | 34 / 37 / 0 / 26 / 3 |
| 58 | Eliminate | controlled_avoid | 8→9 | 1 / 0 | 59 / 33 / 0 / 1 / 6 |
| 60 | Eliminate | natural_legacy | 6→2 | 4 / 4 | 30 / 42 / 1 / 23 / 4 |
| 60 | Eliminate | controlled_avoid | 9→8 | 2 / 0 | 55 / 32 / 0 / 9 / 4 |

## 해석 범위

- 각 작전·조작은 10시드다. 1~2승 차이를 확정적인 난이도 변화로 해석하지 않는다. 지역 합계도 서로 다른 목표를 섞으므로 상세 표와 함께 읽는다.
- 예고 중 멈추는 적은 추격 압박이 줄 수 있다. 전체 승률을 일정하게 만들려고 모든 적 체력·공격력을 일괄 올리지 않는다.
- 고정 Lv.5 장비, 인위적으로 주입한 밀집 화면, 무료/유료 달력 결과는 별도 자료다. 이 표를 사람의 승률·구매 전환·실물 폰 성능으로 사용하지 않는다.

region037-expanded-inputs.json.gz SHA-256: `1305d48e62f4c4cea0cc3d29fd20b276b658d2523c849202ac9f7ff2f77598b8`

region037-expanded-before.json.gz SHA-256: `8a76fef17248588ed3172ca53568d69afd1ade2d0113aa45a5a7ac7a628201d9`

region037-expanded-after.json.gz SHA-256: `6d446d4ae551c16e5ca1b4153257ff56619c365e6105754158edd5c8a2f09bfa`
