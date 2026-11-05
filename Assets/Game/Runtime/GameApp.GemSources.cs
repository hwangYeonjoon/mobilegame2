using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void GemSources(int cost,Action back)
        {
            int missing=Math.Max(0,cost-State.gems);var pack=CommerceCatalog.SmallestPack(missing);
            var card=Dialog("보석 획득 안내","무료·유료 보석 모두 같은 상품에 사용할 수 있어요.",740);
            CurrencyArt.Draw(card,1,32,137,55);
            Ui.Label(card,cost==0?"보유 보석 "+State.gems.ToString("N0"):"필요 "+cost+" · 보유 "+State.gems+" · 부족 "+missing,99,143,369,44,21,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(card,"플레이로 모으기\n누적 출석 7회: 보석 150개\n일일 임무 모두 완료: 하루 30개\n작전 최초 클리어: 작전당 50개\n성장 도전 최고 단계: 주간 50개",24,216,444,179,19,Ui.Ink);
            Ui.Label(card,cost==0?"플레이로 모은 보석도 의상·장비 구매에 사용할 수 있어요.":missing==0?"지금 보유한 보석으로 구매할 수 있어요.":pack==null?"보석 상품 구성은 보급 상점에서 확인하세요.":"부족분을 채우는 최소 묶음: "+pack.Name+"\n실제 가격과 판매 여부는 Google Play에서 확인해요.",24,425,444,80,17,Ui.Muted);
            if(pack!=null)Ui.Button(card,"보석 묶음 구성 보기 · 결제 아님",24,533,444,51,()=>StoreOfferPanel(pack),Ui.Raised,true,17);
            Ui.Button(card,cost==0?"상점으로 돌아가기":"구매 화면으로 돌아가기",24,619,444,53,back,Ui.Mint,true,17);
        }
    }
}
