using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        Image playerArt, muzzle, ultimateFill;
        RectTransform playerShadow, shieldRing;
        Button ultimateButton;
        Text ultimateLabel;
        int renderedUltimate;
        AudioClip ultimateClip;
        void SetupCombatEffects()
        {
            renderedUltimate = 0;
            shieldRing = CombatRing.Create(effectLayer,61,new Color(.4f,.8f,1,.8f),.035f);
            muzzle = Ui.Disc(effectLayer,0,0,15,Ui.Gold);
            muzzle.rectTransform.pivot = new Vector2(.5f,.5f);
            Ui.Box(muzzle.transform,5,-4,5,23,Color.white,false);
        }
        void TriggerUltimate()
        {
            if (combat == null || battlePaused || networkBusy || !combat.UseUltimate()) return;
            recorder.Ultimate(); dragging = false;
            RenderBattle();
        }
        void RenderPlayerMotion()
        {
            float moving = Mathf.Clamp01(combat.LastMovement.Length);
            float stride = Mathf.Sin(combat.Elapsed*17)*moving;
            float recoil = combat.ShotFlash/.11f;
            float scale=StylePresentation.PlayerScale;
            player.anchoredPosition = new Vector2(combat.Position.x-40*scale-Mathf.Cos(combat.Facing)*recoil*2,
                -combat.Position.y+50*scale+Mathf.Abs(stride)*.8f);
            player.localScale=Vector3.Scale(new Vector3(scale,scale,1),GuardianMotion.PoseScale(combat.Elapsed,moving,combat.ShotFlash>0,combat.HurtFlash>0));
            player.localEulerAngles = new Vector3(0,0,-combat.LastMovement.x*5+stride*2-recoil*3);
            playerArt.rectTransform.localScale = new Vector3(Mathf.Cos(combat.Facing)<-.05f ? -1 : 1,1,1);
            int outfit=battleOutfit;
            playerArt.sprite=GuardianMotion.Sample(combat.Hero,State.selectedSkin,outfit,combat.Elapsed,moving,combat.ShotFlash>0,combat.HurtFlash>0,combat.UltimateFlash>0);
            playerArt.color = combat.HurtFlash>0 ? new Color(1,.55f,.5f) : outfit>0?Color.white:CharacterArt.Tint(State.selectedSkin);
            muzzle.color=outfit>0?OutfitVisuals.Accent(outfit):Ui.Gold;
            playerShadow.sizeDelta=new Vector2(40-Mathf.Abs(stride)*4,13-Mathf.Abs(stride));
            playerShadow.anchoredPosition = new Vector2(combat.Position.x-20,-combat.Position.y-12);
            shieldRing.gameObject.SetActive(combat.Shield>0);
            shieldRing.anchoredPosition = new Vector2(combat.Position.x,-combat.Position.y);
            shieldRing.localScale = Vector3.one*(1+Mathf.Sin(combat.Elapsed*4)*.025f);
            muzzle.gameObject.SetActive(combat.ShotFlash>0);
            V2 point = combat.Position+V2.Angle(combat.Facing)*23;
            muzzle.rectTransform.anchoredPosition = new Vector2(point.x,-point.y);
            muzzle.rectTransform.localScale = Vector3.one*(.5f+recoil*.7f);
            muzzle.rectTransform.localEulerAngles = new Vector3(0,0,-combat.Facing*Mathf.Rad2Deg);
            arena.anchoredPosition = BattleCameraOffset()+(visualBudget.Compact?Vector2.zero:combat.UltimateFlash>.3f
                ? new Vector2(Mathf.Sin(combat.Elapsed*95)*2,Mathf.Cos(combat.Elapsed*80)*2) : combat.HurtFlash>0?new Vector2(Mathf.Sin(combat.Elapsed*85),0):Vector2.zero);
            if (combat.UltimateUses != renderedUltimate)
            {
                renderedUltimate = combat.UltimateUses;
                ShowUltimateEffect();
            }
        }
        void RenderUltimateHud()
        {
            if (ultimateButton == null) return;
            ultimateButton.interactable = combat.CanUltimate && !battlePaused;
            ultimateButton.image.color = combat.CanUltimate ? Color.Lerp(Ui.Mint,Ui.Ink,.12f+.12f*Mathf.Sin(Time.unscaledTime*3)) : Ui.Raised;
            ultimateLabel.color = combat.CanUltimate ? Ui.Background : Ui.Muted;
            ultimateLabel.text = Combat.UltimateNames[combat.Hero]+"\n"+(combat.CanUltimate ? "필살기 발동" : combat.UltimateCooldown>0 ? Mathf.CeilToInt(combat.UltimateCooldown)+"초 · "+(int)combat.UltimateCharge+"%" : "충전 "+(int)combat.UltimateCharge+"%");
            ultimateFill.rectTransform.sizeDelta = new Vector2(158*combat.UltimateCharge/100,4);
        }
        void ShowUltimateEffect()
        {
            PulseHaptics(40);
            if (State.sound && audioSource != null)
            {
                if(ultimateClip==null)ultimateClip=CombatSoundBank.Get(9);
                audioSource.PlayOneShot(ultimateClip,.65f);
            }
            var origin = new Vector2(combat.UltimateOrigin.x,-combat.UltimateOrigin.y);
            if(Outfits.Active(State,combat.Hero)>0)OutfitVisuals.Special(effectLayer,origin,240,Outfits.Active(State,combat.Hero));
            Color color = Outfits.Active(State,combat.Hero)>0?OutfitVisuals.Accent(Outfits.Active(State,combat.Hero)):combat.Hero == 1 ? Ui.Blue : combat.Hero == 2 ? Ui.Gold : combat.Hero==3 ? Ui.Gold : combat.Hero==4 ? Ui.Blue : Ui.Mint;
            particles.Burst(combat.UltimateOrigin,combat.Hero==4?32:24,color,combat.Hero==3?220:150,combat.Hero==4?6:4);
            if(combat.Hero==3)
            {
                for(int i=0;i<(visualBudget.Compact?4:8);i++)
                {
                    var arcRoot=Ui.Rect(effectLayer,"Discharge arc",0,0,540,960);
                    var arc=arcRoot.gameObject.AddComponent<LightningArc>();arc.raycastTarget=false;arc.color=color;
                    arc.From=origin;arc.To=origin+new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4))*300;
                    floating.Add(new FloatingEffect{root=arcRoot,life=.65f,total=.65f});
                }
            }
            if (combat.Hero == 1)
            {
                var beam = Ui.Rect(effectLayer,"PhotonBeam",origin.x,-origin.y,560,76);
                beam.pivot = new Vector2(0,.5f);
                beam.localEulerAngles = new Vector3(0,0,-Mathf.Atan2(combat.UltimateDirection.y,combat.UltimateDirection.x)*Mathf.Rad2Deg);
                Ui.Box(beam,0,0,560,76,new Color(color.r,color.g,color.b,.28f),false);
                Ui.Box(beam,0,21,560,34,color,false);
                Ui.Box(beam,0,33,560,10,Color.white,false);
                beam.gameObject.AddComponent<CombatBurst>().WithClock(combat).Configure(.65f,1,1);
            }
            else
            {
                float diameter = combat.Hero == 2 ? 360 : combat.Hero==3 ? 620 : combat.Hero==4 ? 520 : 490;
                for (int i=0;i<(visualBudget.Compact?1:3);i++)
                {
                    var ring = CombatRing.Create(effectLayer,diameter,color,i==0 ? .018f : .035f);
                    ring.anchoredPosition = origin;
                    ring.gameObject.AddComponent<CombatBurst>().WithClock(combat).Configure(.8f+i*.1f,.12f+i*.09f,1);
                }
                for(int i=0;i<(visualBudget.Compact?4:12);i++)
                {
                    float angle=i*Mathf.PI/6;
                    var spark=Ui.Box(effectLayer,origin.x+Mathf.Cos(angle)*70,-origin.y+Mathf.Sin(angle)*70,6,38,color,false).rectTransform;
                    spark.pivot=new Vector2(.5f,.5f); spark.localEulerAngles=new Vector3(0,0,-angle*Mathf.Rad2Deg-90);
                    spark.gameObject.AddComponent<CombatBurst>().WithClock(combat).Configure(.55f,.7f,1.7f);
                }
            }
            var label=Ui.Label(effectLayer,Combat.UltimateNames[combat.Hero],combat.Position.x-190,combat.Position.y-180,380,50,30,color,TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<CombatBurst>().WithClock(combat).Configure(1.1f,1,1);
        }
    }
    public sealed class CombatBurst : MonoBehaviour
    {
        float age,duration,startScale,endScale;
        Graphic[] graphics; Color[] colors;
        Combat clock;float lastTime;
        public CombatBurst WithClock(Combat source){clock=source;lastTime=source.Elapsed;return this;}
        public void Configure(float lifetime,float from,float to)
        {
            duration=lifetime;startScale=from;endScale=to;
            graphics=GetComponentsInChildren<Graphic>();colors=new Color[graphics.Length];
            for(int i=0;i<graphics.Length;i++)colors[i]=graphics[i].color;
            transform.localScale=Vector3.one*from;
        }
        void Update()
        {
            if(graphics==null)return;
            float delta=clock==null?Time.deltaTime:Mathf.Max(0,clock.Elapsed-lastTime);
            if(clock!=null)lastTime=clock.Elapsed;
            age+=delta;float t=Mathf.Clamp01(age/duration);
            transform.localScale=Vector3.one*Mathf.Lerp(startScale,endScale,1-(1-t)*(1-t));
            for(int i=0;i<graphics.Length;i++) { var c=colors[i];c.a*=1-t;graphics[i].color=c; }
            if(age>=duration)Destroy(gameObject);
        }
    }
}
