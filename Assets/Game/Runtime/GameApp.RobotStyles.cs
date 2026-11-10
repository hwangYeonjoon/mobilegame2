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
            string details=style==1?"기체 외형 3종 · 600 보석 · 계정당 1회\n구매하면 3기체 외형을 즉시 적용\n성장 재료 없음 · 공격력과 체력은 그대로":style==2?"선택한 기체 1종을 재료로 제작하고 적용\n작전 42 완료 · 골드 30,000 · 부품 300\n코어 파편 12개 · 주간 도전에서 획득":"기본 외형으로 돌아갑니다.";
            Ui.Label(card,details,24,566,444,99,17,Ui.Ink,TextAnchor.MiddleCenter);
            string button=owned?(RobotStyles.Active(State,frame)==style?"적용 중":"이 기체에 적용"):style==1?"성운 기사 팩 · 외형 구성 확인":"재료를 사용해 제작";
            Ui.Button(card,button,24,685,444,54,()=>{
                if(owned)Change(new GameCommand("robot_style_equip"){frame=frame,skin=style},confirmed:()=>RobotStylePanel(frame,style));
                else if(style==1)RobotPackConfirm(frame);
                else {var confirm=Dialog("회수 선봉대 제작",details,390);Ui.Button(confirm,"골드·부품·파편 사용 후 적용",24,180,444,56,()=>Change(new GameCommand("robot_style_craft"){frame=frame},confirmed:()=>RobotStylePanel(frame,style)),Ui.Mint,true,18);Ui.Button(confirm,"취소",24,265,444,48,()=>RobotStylePanel(frame,style),Ui.Raised,true,18);}
            },Ui.Gold,!(owned&&RobotStyles.Active(State,frame)==style),18);
            Ui.Button(card,"집중 강화",24,762,214,50,()=>GrowthPathPanel(),Ui.Mint,true,18);
            Ui.Button(card,"로봇 전시",254,762,214,50,()=>RobotHangar(),Ui.Raised,true,18);
            Ui.Label(card,"능력치는 동일 · 매판 조립한 기체와 전시에 적용",24,829,444,32,13,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void RobotPackConfirm(int frame)
        {
            var card=Dialog("성운 기사 팩 · 구매 확인","600 보석 · 외형 전용 · 계정당 1회\n포격·돌파·철벽 외형을 즉시 적용",720);
            for(int i=0;i<3;i++)
            {
                RobotRigView.Draw(card,26+i*150,160,140,i*4+1,2,1);
                Ui.Label(card,RobotAssembly.Frames[i],24+i*150,310,144,36,15,Ui.Gold,TextAnchor.MiddleCenter);
            }
            Ui.Label(card,"실루엣 · 공격 강조색 · 합체 등장 연출\n전투와 내 로봇 전시에 함께 적용돼요.",24,371,444,70,20,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"골드·부품 제공 없음 · 능력치 변화 없음\n성장 보급과 별개의 상품이에요.",24,465,444,60,18,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Label(card,"보유 보석 "+State.gems+" · 가격 "+RobotStyles.PackPrice,24,535,444,36,18,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,"600 보석 구매 · 외형 3종 적용",24,582,444,55,()=>Change(new GameCommand("buy"){value="robot_astral_pack"},confirmed:()=>RobotStylePanel(frame,1)),Ui.Gold,!RobotStyles.Owned(State,0,1)&&State.gems>=RobotStyles.PackPrice&&State.gemDebt==0,18);
            Ui.Button(card,"취소",24,654,214,47,()=>RobotStylePanel(frame,1),Ui.Raised,true,18);
            Ui.Button(card,"보석 획득·충전",254,654,214,47,()=>GemSources(RobotStyles.PackPrice,()=>RobotPackConfirm(frame)),Ui.Raised,true,17);
        }
    }
}
