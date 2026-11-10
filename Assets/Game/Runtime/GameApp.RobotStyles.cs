using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void GrowthPathPanel(string feedback=null)
        {
            var card=Dialog("원하는 성장을 바로 적용",feedback??"한 경로를 선택해 여러 단계를 강화합니다.\n표시한 재료를 사용하며 다른 경로는 다시 계산해요.",850);
            for(int slot=0;slot<4;slot++)
            {
                var q=GrowthPaths.Quote(State,slot);float y=145+slot*132;
                if(q==null){Ui.Label(card,new[]{"무기","방어구","모듈","대원"}[slot]+" · 최대 레벨",24,y,444,50,20,Ui.Muted);continue;}
                string delta=slot==2?Growth.ModuleEffect(State.items.Find(i=>i.id==q.id))+" → "+Growth.ModuleEffect(State.items.Find(i=>i.id==q.id),q.count):"공격 "+Catalog.Attack(State)+" → "+q.attack+" · 체력 "+Catalog.Health(State)+" → "+q.health;
                Ui.Label(card,q.name+" Lv."+q.from+" → "+q.to+"\n"+delta,24,y,444,56,18,Ui.Mint);
                Ui.Button(card,q.count==0?"재료 부족":"골드 "+q.gold+" · 부품 "+q.parts+" 사용해 "+q.count+"단계 강화",24,y+65,444,48,()=>{
                    var confirm=Dialog("집중 강화 확인",q.name+" Lv."+q.from+" → "+q.to+"\n"+delta,390);
                    Ui.Button(confirm,"골드 "+q.gold+" · 부품 "+q.parts+" 사용",24,163,444,60,()=>Change(new GameCommand("upgrade_path",q.id){stage=q.slot,count=q.count,level=q.from},confirmed:()=>GrowthPathPanel("강화 완료 · "+delta)),Ui.Mint,true,18);
                    Ui.Button(confirm,"취소",24,248,444,50,()=>GrowthPathPanel(),Ui.Raised,true,18);
                },Ui.Raised,q.count>0,16);
            }
            Ui.Button(card,"무료 현장 보급",24,695,214,50,SalvagePanel,Ui.Mint,true,17);
            Ui.Button(card,"작전 패스",254,695,214,50,GrowthPassPanel,Ui.Gold,true,17);
            Ui.Button(card,"성장 화면",24,765,444,48,GrowthPanel,Ui.Raised,true,18);
        }
        void RobotStylePanel(int frame=0,int style=1)
        {
            bool owned=RobotStyles.Owned(State,frame,style);
            var card=Dialog("로봇 외형 공방","실루엣과 합체 연출을 바꿉니다. 외형 자체에는 능력치가 없어요.",900);
            for(int i=0;i<3;i++){int f=i;Ui.Button(card,RobotAssembly.Frames[i],24+i*150,128,144,43,()=>RobotStylePanel(f,style),frame==i?Ui.Mint:Ui.Raised,true,15);}
            RobotRigView.Draw(card,116,185,260,frame*4+1,2,style);
            Ui.Label(card,RobotStyles.Names[style],24,450,444,36,25,Ui.Gold,TextAnchor.MiddleCenter);
            for(int i=0;i<3;i++){int v=i;Ui.Button(card,RobotStyles.Names[i],24+i*150,503,144,43,()=>RobotStylePanel(frame,v),style==i?Ui.Mint:Ui.Raised,true,16);}
            string details=style==1?"기체 외형 3종 + 골드 18,000 + 부품 180\n900 보석 · 계정당 1회 · 구매하면 외형 즉시 적용\n재료를 사용한 강화는 별도 선택 · 최대 레벨은 동일":style==2?"선택한 기체 1종을 재료로 제작하고 적용\n작전 42 완료 · 골드 30,000 · 부품 300\n코어 파편 12개 · 주간 도전에서 획득":"기본 외형으로 돌아갑니다.";
            Ui.Label(card,details,24,566,444,99,17,Ui.Ink,TextAnchor.MiddleCenter);
            string button=owned?(RobotStyles.Active(State,frame)==style?"적용 중":"이 기체에 적용"):style==1?"성운 기사 팩 구성·강화 미리보기":"재료를 사용해 제작";
            Ui.Button(card,button,24,685,444,54,()=>{
                if(owned)Change(new GameCommand("robot_style_equip"){frame=frame,skin=style},confirmed:()=>RobotStylePanel(frame,style));
                else if(style==1)RobotPackConfirm(frame);
                else {var confirm=Dialog("회수 선봉대 제작",details,390);Ui.Button(confirm,"골드·부품·파편 사용 후 적용",24,180,444,56,()=>Change(new GameCommand("robot_style_craft"){frame=frame},confirmed:()=>RobotStylePanel(frame,style)),Ui.Mint,true,18);Ui.Button(confirm,"취소",24,265,444,48,()=>RobotStylePanel(frame,style),Ui.Raised,true,18);}
            },Ui.Gold,!(owned&&RobotStyles.Active(State,frame)==style),18);
            Ui.Button(card,"집중 강화",24,762,214,50,()=>GrowthPathPanel(),Ui.Mint,true,18);
            Ui.Button(card,"로봇 전시",254,762,214,50,()=>RobotHangar(),Ui.Raised,true,18);
            Ui.Label(card,"부품 외형은 매판 조립한 기체에 적용 · 재료는 성장에 영향",24,829,444,32,13,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void RobotPackConfirm(int frame)
        {
            var card=Dialog("성운 기사 팩 · 구매 확인","900 보석 · 외형 3종 즉시 적용\n골드 18,000 + 부품 180 · 강화는 직접 선택",730);
            Ui.Label(card,"구매 후 강화 · 보유 재료 포함 · 한 경로 기준",24,141,444,34,18,Ui.Mint);
            for(int slot=0;slot<4;slot++)
            {
                var q=GrowthPaths.Quote(State,slot,30,RobotStyles.PackGold,RobotStyles.PackParts);
                string t=q==null?new[]{"무기","방어구","모듈","대원"}[slot]+" · 최대 레벨 (재료로 추가 강화 불가)":q.name+" Lv."+q.from+" → "+q.to+"\n"+(slot==2?Growth.ModuleEffect(State.items.Find(i=>i.id==q.id),q.count):"공격 "+Catalog.Attack(State)+" → "+q.attack+" · 체력 "+Catalog.Health(State)+" → "+q.health);
                Ui.Label(card,t,24,196+slot*75,444,64,17,Ui.Ink);
            }
            Ui.Label(card,"보유 보석 "+State.gems+" → "+(State.gems-RobotStyles.PackPrice)+"\n무료 보석도 사용 가능 · 모든 경로가 함께 강화되지는 않아요.",24,504,444,56,16,Ui.Muted);
            Ui.Button(card,"900 보석 구매 · 외형 3종 적용",24,579,444,55,()=>Change(new GameCommand("buy"){value="robot_astral_pack"},confirmed:()=>GrowthPathPanel("성운 기사 적용 완료 · 받은 재료로 강화할 경로를 고르세요.")),Ui.Gold,!RobotStyles.Owned(State,0,1)&&State.gems>=900&&State.gemDebt==0,18);
            Ui.Button(card,"취소",24,656,214,47,()=>RobotStylePanel(frame,1),Ui.Raised,true,18);
            Ui.Button(card,"보석 획득·충전",254,656,214,47,()=>GemSources(900,()=>RobotPackConfirm(frame)),Ui.Raised,true,17);
        }
    }
}
