using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        bool OutfitForSale(int id)
        {
            if(!Online)return true;DateTimeOffset now;if(!cloudReady||!cloud.ContentTime(out now))return false;
            var item=cloud.Content.outfits.Find(x=>x.id==id);return item!=null&&item.Available(now,AppRelease.Build);
        }
        string OutfitSaleLabel(int id)
        {
            if(!Online)return "◆ 600 · 확정 구매";DateTimeOffset now;
            if(!cloudReady||!cloud.ContentTime(out now))return "판매 일정 확인 필요";
            var item=cloud.Content.outfits.Find(x=>x.id==id);if(item==null)return "공개 준비 중";
            if(item.paused)return "판매 준비 중 · 체험 가능";
            if(item.minimumBuild>AppRelease.Build)return "앱 업데이트 후 구매 가능";
            DateTimeOffset start,end;
            if(DateTimeOffset.TryParse(item.startsAt,out start)&&now<start)return start.ToOffset(TimeSpan.FromHours(9)).ToString("MM/dd HH:mm")+" 공개 (한국)";
            if(DateTimeOffset.TryParse(item.endsAt,out end))return now>=end?"판매 종료 · 체험 가능":end.ToOffset(TimeSpan.FromHours(9)).ToString("MM/dd HH:mm")+"까지 (한국)";
            return "◆ 600 · 확정 구매";
        }
        void RefreshOutfitCatalog(Action redraw)
        {if(Online&&cloudReady)StartCoroutine(cloud.RefreshContent(redraw));}
        void OnlineMetricsPanel()
        {
            bool enabled=Online&&cloudReady&&cloud.Latest!=null&&cloud.Latest.metricsConsent;
            var card=Dialog("온라인 의상 체험 통계","선택 동의 · 플레이 테스트 파일과 별도 설정",650);
            Ui.Label(card,"동의하면 미보유 의상의 체험 여부를 서버로 보내요.\n계정·의상별 하루 1회, 최대 90일 보관합니다.\n체험 후 7일 내 구매 여부를 집계해\n새 의상 제작과 상품 개선에 사용해요.",24,140,444,146,17,Ui.Ink);
            Ui.Label(card,"동의하지 않아도 게임과 구매에 차이가 없어요.\n끄면 서버의 체험 기록이 삭제됩니다.\n구매·재화 원장과 현재 장착 통계는\n게임 서비스 운영을 위해 별도로 집계합니다.",24,305,444,130,16,Ui.Muted);
            Ui.Button(card,enabled?"체험 통계 끄기 · 기록 삭제":"체험 통계에 동의하고 켜기",24,469,444,56,()=>Change(new GameCommand("metrics_consent"){enabled=!enabled},OnlineMetricsPanel),Ui.Mint,Online&&cloudReady,17);
            Ui.Label(card,Online?"현재 상태: "+(enabled?"동의함":"동의하지 않음"):"온라인 계정 연결 후 설정할 수 있어요.",24,554,444,42,16,Ui.Muted,TextAnchor.MiddleCenter);
        }
    }
}
