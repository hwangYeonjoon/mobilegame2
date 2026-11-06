using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        BattleVisualBudget visualBudget;
        CanvasGroup effectOpacity;
        RectTransform identityLayer;
        readonly int[] displayedSkills={-1,-1,-1,-1,-1,-1,-1,-1};
        int battleOutfit,displayedHp=-1,displayedMaxHp=-1,displayedKills=-1,displayedSeconds=-1;
        bool displayedShield;
        readonly System.Collections.Generic.Stack<RectTransform> textEffects=new System.Collections.Generic.Stack<RectTransform>();
        readonly System.Collections.Generic.Stack<RectTransform> ringEffects=new System.Collections.Generic.Stack<RectTransform>();
        readonly System.Collections.Generic.Stack<RectTransform> arcEffects=new System.Collections.Generic.Stack<RectTransform>();
        void SetupBattleClarity()
        {
            visualBudget=new BattleVisualBudget{Mode=Mathf.Clamp(PlayerPrefs.GetInt("scrapguard.visual-mode.v1",0),0,2)};
            effectOpacity=effectLayer.gameObject.AddComponent<CanvasGroup>();effectOpacity.blocksRaycasts=false;
            battleOutfit=Outfits.Active(State,combat.Hero);
            displayedHp=displayedMaxHp=displayedKills=displayedSeconds=-1;
            for(int i=0;i<8;i++)displayedSkills[i]=-1;
            textEffects.Clear();ringEffects.Clear();arcEffects.Clear();
            // Player stays visible above friendly VFX; every danger boundary remains above the player.
            identityLayer=Ui.Rect(arena,"Identity actors",0,0,540,960);
            player.SetParent(identityLayer,false);
            hazardLayer.SetAsLastSibling();playerLocator.SetAsLastSibling();
            for(int frame=0;frame<6;frame++)GuardianMotion.Frame(combat.Hero,State.selectedSkin,frame);
            string look=StylePresentation.Look(battleOutfit,State.selectedSkin);
            Ui.Label(screen,Catalog.HeroNames[combat.Hero]+" · "+look,24,88,492,22,14,battleOutfit>0?OutfitVisuals.Accent(battleOutfit):Ui.Mint);
        }
        void UpdateVisualBudget()
        {
            visualBudget.Sample(smokeCapturing||battlePaused?0:Time.unscaledDeltaTime,combat.Enemies.Count,combat.Bullets.Count);
            particles.Limit=visualBudget.ParticleLimit;
            effectOpacity.alpha=visualBudget.Compact?.48f:.8f;
        }
        void BattleDisplayOptions()
        {
            bool inBattle=combat!=null;
            if(inBattle)battlePaused=true;
            int selected=visualBudget!=null&&inBattle?visualBudget.Mode:PlayerPrefs.GetInt("scrapguard.visual-mode.v1",0);
            var card=Dialog("전투 화면 설정","위험 예고·적·탄환·피해 판정은 모든 설정에서 같아요.",580);
            string[] names={"자동 · 혼잡하거나 느려지면 효과 줄이기","풍부하게 · 기본 연출 유지","간결하게 · 섬광과 화면 흔들림 줄이기"};
            for(int i=0;i<3;i++)
            {
                int mode=i;
                Ui.Button(card,names[i],24,143+i*86,444,72,()=>{
                    PlayerPrefs.SetInt("scrapguard.visual-mode.v1",mode);PlayerPrefs.Save();
                    if(visualBudget!=null)visualBudget.Mode=mode;
                    BattleDisplayOptions();
                },selected==i?Ui.Mint:Ui.Raised,true,19);
            }
            Ui.Label(card,"의상 실루엣과 전용 탄환은 간결 모드에서도 유지해요.\n이 설정은 현재 기기에 저장돼요.",24,414,444,61,18,Ui.Muted);
            System.Action back=()=>{if(inBattle)PauseBattle();else Settings();};
            Ui.Button(card,"돌아가기",24,499,444,58,back,Ui.Raised,true,20);
            var close=card.GetChild(2).GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(()=>back());
        }
        RectTransform TakeEffect(int type)
        {
            var pool=type==1?textEffects:type==2?arcEffects:ringEffects;
            RectTransform root;
            if(pool.Count>0)root=pool.Pop();
            else if(type==1)root=Ui.Label(effectLayer,"",0,0,140,35,16).rectTransform;
            else if(type==2){root=Ui.Rect(effectLayer,"Chain arc",0,0,540,960);root.gameObject.AddComponent<LightningArc>().raycastTarget=false;}
            else root=CombatRing.Create(effectLayer,20,Color.white);
            root.gameObject.SetActive(true);root.localScale=Vector3.one;root.localEulerAngles=Vector3.zero;return root;
        }
        void ReturnEffect(FloatingEffect effect)
        {
            if(effect.pooledType<0){Destroy(effect.root.gameObject);return;}
            effect.root.gameObject.SetActive(false);
            var pool=effect.pooledType==1?textEffects:effect.pooledType==2?arcEffects:ringEffects;
            if(pool.Count<36)pool.Push(effect.root);else Destroy(effect.root.gameObject);
        }
    }
}
