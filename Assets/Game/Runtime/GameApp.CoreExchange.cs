using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void CoreExchange()
        {
            var card=Dialog("균열 파편 교환소","코어 균열에서 획득한 파편으로만 교환할 수 있어요.",690);
            Ui.Label(card,"보유 코어 파편  "+State.coreShards,24,140,444,49,25,Ui.Hex("C89BFF"),TextAnchor.MiddleCenter);
            Ui.Box(card,24,208,444,156,Ui.Raised);
            Ui.Label(card,"칭호 · 균열의 개척자",40,222,410,36,23,Ui.Ink);
            Ui.Label(card,"내 프로필과 공유 카드에서 보여 주세요.",40,266,410,30,15,Ui.Muted);
            Ui.Button(card,State.ownedCosmetics.Contains("title_rift")?"보유 중":"파편 12개로 교환",40,309,410,41,()=>ConfirmCoreExchange("title_rift","균열의 개척자",12),Ui.Raised,!State.ownedCosmetics.Contains("title_rift"),17);
            Ui.Box(card,24,385,444,156,Ui.Raised);
            Ui.Label(card,"프레임 · 코어 궤도",40,399,410,36,23,Ui.Hex("C89BFF"));
            Ui.Label(card,"보랏빛 코어 궤도로 대원의 전시 카드를 꾸며요.",40,443,410,30,15,Ui.Muted);
            Ui.Button(card,State.ownedCosmetics.Contains("frame_core")?"보유 중":"파편 20개로 교환",40,486,410,41,()=>ConfirmCoreExchange("frame_core","코어 궤도",20),Ui.Raised,!State.ownedCosmetics.Contains("frame_core"),17);
            Ui.Label(card,"주간 달성 보상으로 파편 최대 17개 획득\n기간이 바뀌어도 모은 파편은 사라지지 않아요.\n보석 구매·장비 판매·일반 전투에서는 얻을 수 없어요.",24,566,444,92,15,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void ConfirmCoreExchange(string offer,string name,int price)
        {
            var card=Dialog("교환할까요?",name+" · 확정 구성 · 능력치 변화 없음",340);
            Ui.Label(card,"코어 파편 "+price+"개 사용\n현재 보유 "+State.coreShards+"개",24,126,444,70,23,Ui.Hex("C89BFF"),TextAnchor.MiddleCenter);
            Ui.Button(card,"교환 확정",24,245,444,60,()=>Change(new GameCommand("core_exchange"){value=offer},CoreExchange),Ui.Mint,State.coreShards>=price,19);
        }
    }
}
