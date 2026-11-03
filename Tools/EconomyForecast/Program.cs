using System.Text.Json;
using ScrapGuard.Core;
var reports=new List<object>();
foreach(bool missions in new[]{false,true})
{
    var now=new DateTimeOffset(2026,1,5,6,0,0,TimeSpan.Zero);
    var state=GameState.New();var economy=new Economy(state,()=>now);
    var checkpoints=new List<object>();int first600=0;
    for(int day=1;day<=90;day++)
    {
        economy.RefreshDay();economy.Attend();
        // Scenario assumption only: no simulated combat, retention, conversion or revenue claims.
        if(missions){state.dailyRuns=3;state.dailyKills=500;economy.ClaimMissions();}
        if(first600==0&&state.gems>=600)first600=day;
        if(day==30||day==60||day==90)checkpoints.Add(new {day,gems=state.gems,gold=state.gold,parts=state.parts,affordable600GemOutfits=state.gems/600});
        now=now.AddDays(1);
    }
    reports.Add(new{scenario=missions?"매일 출석+일일 임무 완료":"매일 출석만",first600GemDay=first600,checkpoints});
}
Console.WriteLine(JsonSerializer.Serialize(new {
    scope="현재 코어 규칙으로 계산한 누적 수급 시나리오. 플레이·첫 클리어·주간·광고·결제 보상과 소비는 제외. 골드·부품에는 시작 재화 포함.",
    notMeasured="달성 가능성, 구매 전환, 잔존율, 매출은 예측하지 않음",reports
},new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
