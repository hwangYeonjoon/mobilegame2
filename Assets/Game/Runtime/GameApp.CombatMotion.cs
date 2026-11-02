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
            player.anchoredPosition = new Vector2(combat.Position.x-20-Mathf.Cos(combat.Facing)*recoil*2,
                -combat.Position.y+30+Mathf.Abs(stride)*2.5f);
            player.localScale = new Vector3(.48f*(1+stride*.035f),.48f*(1-stride*.035f),1);
            player.localEulerAngles = new Vector3(0,0,-combat.LastMovement.x*5+stride*2-recoil*3);
            playerArt.rectTransform.localScale = new Vector3(Mathf.Cos(combat.Facing)<-.05f ? -1 : 1,1,1);
            playerArt.color = combat.HurtFlash>0 ? new Color(1,.55f,.5f) : Color.white;
            playerShadow.anchoredPosition = new Vector2(combat.Position.x-20,-combat.Position.y-12);
            shieldRing.gameObject.SetActive(combat.Shield>0);
            shieldRing.anchoredPosition = new Vector2(combat.Position.x,-combat.Position.y);
            shieldRing.localScale = Vector3.one*(1+Mathf.Sin(combat.Elapsed*4)*.025f);
            muzzle.gameObject.SetActive(combat.ShotFlash>0);
            V2 point = combat.Position+V2.Angle(combat.Facing)*23;
            muzzle.rectTransform.anchoredPosition = new Vector2(point.x,-point.y);
            muzzle.rectTransform.localScale = Vector3.one*(.5f+recoil*.7f);
            muzzle.rectTransform.localEulerAngles = new Vector3(0,0,-combat.Facing*Mathf.Rad2Deg);
            arena.anchoredPosition = combat.UltimateFlash>.3f
                ? new Vector2(Mathf.Sin(combat.Elapsed*95)*2,Mathf.Cos(combat.Elapsed*80)*2) : Vector2.zero;
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
            ultimateButton.image.color = combat.CanUltimate ? Ui.Mint : Ui.Raised;
            ultimateLabel.color = combat.CanUltimate ? Ui.Background : Ui.Muted;
            ultimateLabel.text = Combat.UltimateNames[combat.Hero]+"\n"+(combat.CanUltimate ? "필살기 발동" : combat.UltimateCooldown>0 ? Mathf.CeilToInt(combat.UltimateCooldown)+"초 · "+(int)combat.UltimateCharge+"%" : "충전 "+(int)combat.UltimateCharge+"%");
            ultimateFill.rectTransform.sizeDelta = new Vector2(158*combat.UltimateCharge/100,4);
        }
        void ShowUltimateEffect()
        {
            if (State.sound && audioSource != null)
            {
                if (ultimateClip == null)
                {
                    const int sampleRate=22050;var samples=new float[11025];float phase=0;
                    for(int i=0;i<samples.Length;i++)
                    {
                        float t=i/(float)samples.Length;phase+=Mathf.PI*2*Mathf.Lerp(180,46,t)/sampleRate;
                        samples[i]=(Mathf.Sin(phase)+.18f*Mathf.Sin(phase*4))*.24f*(1-t)*Mathf.Min(1,t*40);
                    }
                    ultimateClip=AudioClip.Create("Core discharge",samples.Length,1,sampleRate,false);ultimateClip.SetData(samples,0);
                }
                audioSource.PlayOneShot(ultimateClip);
            }
            var origin = new Vector2(combat.UltimateOrigin.x,-combat.UltimateOrigin.y);
            Color color = combat.Hero == 1 ? Ui.Blue : combat.Hero == 2 ? Ui.Gold : Ui.Mint;
            if (combat.Hero == 1)
            {
                var beam = Ui.Rect(effectLayer,"PhotonBeam",origin.x,-origin.y,560,76);
                beam.pivot = new Vector2(0,.5f);
                beam.localEulerAngles = new Vector3(0,0,-Mathf.Atan2(combat.UltimateDirection.y,combat.UltimateDirection.x)*Mathf.Rad2Deg);
                Ui.Box(beam,0,0,560,76,new Color(.2f,.65f,1,.28f),false);
                Ui.Box(beam,0,21,560,34,Ui.Blue,false);
                Ui.Box(beam,0,33,560,10,Color.white,false);
                beam.gameObject.AddComponent<CombatBurst>().Configure(.65f,1,1);
            }
            else
            {
                float diameter = combat.Hero == 2 ? 360 : 490;
                for (int i=0;i<3;i++)
                {
                    var ring = CombatRing.Create(effectLayer,diameter,color,i==0 ? .018f : .035f);
                    ring.anchoredPosition = origin;
                    ring.gameObject.AddComponent<CombatBurst>().Configure(.8f+i*.1f,.12f+i*.09f,1);
                }
                for(int i=0;i<12;i++)
                {
                    float angle=i*Mathf.PI/6;
                    var spark=Ui.Box(effectLayer,origin.x+Mathf.Cos(angle)*70,-origin.y+Mathf.Sin(angle)*70,6,38,color,false).rectTransform;
                    spark.pivot=new Vector2(.5f,.5f); spark.localEulerAngles=new Vector3(0,0,-angle*Mathf.Rad2Deg-90);
                    spark.gameObject.AddComponent<CombatBurst>().Configure(.55f,.7f,1.7f);
                }
            }
            var label=Ui.Label(effectLayer,Combat.UltimateNames[combat.Hero],80,266,380,50,30,color,TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<CombatBurst>().Configure(1.1f,1,1);
        }
    }
    public sealed class CombatBurst : MonoBehaviour
    {
        float age,duration,startScale,endScale;
        Graphic[] graphics; Color[] colors;
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
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/duration);
            transform.localScale=Vector3.one*Mathf.Lerp(startScale,endScale,1-(1-t)*(1-t));
            for(int i=0;i<graphics.Length;i++) { var c=colors[i];c.a*=1-t;graphics[i].color=c; }
            if(age>=duration)Destroy(gameObject);
        }
    }
}
