using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        RectTransform taskActor,taskHud;
        Image taskFill;
        Text taskLabel;
        Button taskChoiceButton;
        void SetupFieldTask()
        {
            taskActor=null;taskHud=null;taskChoiceButton=null;
            if(combat.Task==FieldTask.None)return;
            taskActor=Ui.Rect(pickupLayer,"Field objective",0,0,0,0);
            var ring=CombatRing.Create(taskActor,210,new Color(.35f,.85f,1,.35f),.012f);ring.anchoredPosition=Vector2.zero;
            var art=Ui.Box(taskActor,-31,-31,62,62,new Color(.7f,.9f,1,.9f),false);art.sprite=combat.Task==FieldTask.Escort?ArenaArt.Prop(1):ArenaArt.Landmark(7);art.preserveAspect=true;
            Ui.Label(taskActor,combat.TaskTitle,-90,-67,180,26,15,Ui.Blue,TextAnchor.MiddleCenter);
            taskHud=Ui.Box(screen,24,237,492,62,new Color(.03f,.06f,.09f,.85f)).rectTransform;
            taskLabel=Ui.Label(taskHud,"",10,5,472,46,13,Ui.Ink,TextAnchor.MiddleCenter);
            taskFill=Ui.Box(taskHud,10,55,0,3,Ui.Blue,false);
            taskChoiceButton=Ui.Button(screen,"접근해서 회수 방법 선택",120,757,300,49,OpenFieldChoice,Ui.Blue,false,16);
        }
        void RenderFieldTask()
        {
            if(taskHud==null)return;
            taskHud.gameObject.SetActive(combat.Elapsed<75 || combat.CanAssemble);
            taskLabel.text=combat.TaskInstruction;
            taskFill.rectTransform.sizeDelta=new Vector2(472*combat.TaskProgress,3);
            taskActor.gameObject.SetActive(combat.TaskActive);
            var p=combat.TaskPosition;taskActor.anchoredPosition=new Vector2(p.x,-p.y);
            if(taskChoiceButton!=null)
            {
                taskChoiceButton.gameObject.SetActive(combat.CanAssemble || combat.Task==FieldTask.Salvage && combat.TaskActive && combat.TaskChoice==0);
                taskChoiceButton.interactable=combat.CanChooseTask || combat.CanAssemble;
                taskChoiceButton.GetComponentInChildren<Text>().text=combat.CanAssemble?"부품 확보 · 현장 개조 선택":combat.CanChooseTask?"회수 방법 선택":"회수 장치의 푸른 원으로 이동";
            }
        }
        void OpenFieldChoice()
        {
            if(combat.CanAssemble && !battlePaused){FieldAssemblyPanel();return;}
            if(!combat.CanChooseTask || battlePaused)return;
            battlePaused=true;dragging=false;
            var card=Dialog("어떻게 회수할까요?","선택하는 동안 전투가 멈춥니다.\n완료 즉시 현장 개조 1개 선택 · 추가 재화는 승리 시 지급",620);
            Ui.Label(card,"안전 회수\n체력 12% 수리 · 골드 +200 / 부품 +2",24,145,444,90,20,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,"안전하게 회수",24,244,444,54,()=>SelectFieldChoice(1),Ui.Mint,true,18);
            Ui.Label(card,"과부하 회수\n20초 생존 · 화력 +20%, 받는 피해 +20%\n적 증가 · 골드 +400 / 부품 +4",24,320,444,102,18,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card,"위험을 감수하고 회수",24,436,444,54,()=>SelectFieldChoice(2),Ui.Gold,true,18);
            Ui.Button(card,"나중에 결정",24,526,444,49,()=>{battlePaused=false;CloseModal();},Ui.Raised,true,16);
            card.GetChild(2).gameObject.SetActive(false);
        }
        void FieldAssemblyPanel()
        {
            if(!combat.CanAssemble)return;
            battlePaused=true;dragging=false;
            var card=Dialog("회수한 부품으로 개조", "한 가지를 골라 지금 장착하세요.\n이번 전투에서만 적용 · 재화 소모 없음",740);
            for(int i=1;i<=3;i++)
            {
                int module=i;float y=143+(i-1)*157;
                Ui.Label(card,FieldEngineering.Name(i,combat.Hero),24,y,444,35,23,Ui.Mint);
                Ui.Label(card,FieldEngineering.Detail(i,combat.Hero),24,y+40,444,54,16,Ui.Muted);
                Ui.Button(card,"장착 · "+FieldEngineering.Name(i,combat.Hero),24,y+100,444,44,()=>SelectFieldModule(module),Ui.Raised,true,16);
            }
            Ui.Button(card,"나중에 장착",24,652,444,49,()=>{battlePaused=false;CloseModal();},Ui.Raised,true,16);
            card.GetChild(2).gameObject.SetActive(false);
        }
        void FieldEngineeringGuide()
        {
            var card=Dialog("현장 개조 안내","회수·방어·호위 완료 → 부품 확보 → 개조 1개 장착\n재화 소모 없이 이번 전투에서만 사용합니다.",740);
            for(int i=1;i<=3;i++)
            {
                float y=145+(i-1)*153;
                Ui.Label(card,FieldEngineering.Name(i,State.selectedHero),24,y,444,35,23,Ui.Mint);
                Ui.Label(card,FieldEngineering.Detail(i,State.selectedHero),24,y+44,444,70,17,Ui.Muted);
            }
            Ui.Label(card,"대원마다 전용 개조가 달라져요. 대원 선택 후 확인하세요.\n스킬 조합으로 같은 개조를 완성해도 효과는 중복되지 않아요.",24,614,444,65,15,Ui.Muted);
        }
        void SelectFieldModule(int module)
        {
            if(combat.Assemble(module)){recorder.Assemble(module);Track("field_assembly","module_"+module,combat.Hero);}
            battlePaused=false;CloseModal();RenderBattle();
        }
        void SelectFieldChoice(int choice)
        {
            if(combat.ChooseTask(choice))recorder.Encounter(choice);
            battlePaused=false;CloseModal();RenderBattle();
        }
    }
}
