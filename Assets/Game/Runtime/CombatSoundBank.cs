using UnityEngine;
namespace ScrapGuard
{
    // Layered transients, resonant metal and filtered noise. Cached once; never allocates during combat.
    public static class CombatSoundBank
    {
        static readonly AudioClip[] clips=new AudioClip[12];
        public static AudioClip Get(int id)
        {
            id=Mathf.Clamp(id,0,11);if(clips[id]!=null)return clips[id];
            const int rate=22050;float duration=id==1?.24f:id==3?.42f:.12f;
            var data=new float[Mathf.CeilToInt(rate*duration)];uint seed=(uint)(id+1)*19391u;float filtered=0,phase=0;
            float pitch=id<4?new[]{420f,130f,1100f,520f}[id]:new[]{240f,720f,160f,1300f,1800f,450f,900f,210f}[id-4];
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate,u=t/duration;seed=seed*1664525u+1013904223u;
                float noise=((seed>>8)/(float)0xffffff)*2-1;filtered+=(noise-filtered)*.23f;
                phase+=2*Mathf.PI*pitch*Mathf.Lerp(1.3f,id==3?1.8f:.55f,u)/rate;
                float strike=noise*Mathf.Exp(-t*160)*.33f;
                float metal=(Mathf.Sin(phase)+.35f*Mathf.Sin(phase*2.73f))*.29f*Mathf.Exp(-t*(id==1?15:25));
                float tail=filtered*(id==1?.48f:.16f)*(1-u)*(1-u);
                float envelope=Mathf.Min(1,t*700)*Mathf.Min(1,(duration-t)*120);
                data[i]=Mathf.Clamp((strike+metal+tail)*envelope,-.7f,.7f);
            }
            clips[id]=AudioClip.Create("Scrap material "+id,data.Length,1,rate,false);clips[id].SetData(data,0);return clips[id];
        }
    }
}
