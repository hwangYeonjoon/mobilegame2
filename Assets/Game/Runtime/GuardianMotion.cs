using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public static class GuardianMotion
    {
        static readonly string[] Names={"Luke","Ray","Volt","Spark","Iris"};
        static readonly Sprite[,,] Frames=new Sprite[HeroCatalog.Count,3,6];
        public static Sprite Frame(int hero,int skin,int frame)
        {
            if(!HeroCatalog.Valid(hero)||skin<0||frame<0||frame>=6)return null;
            int paint=skin<3?skin:0;
            if(Frames[hero,paint,frame]==null)Frames[hero,paint,frame]=ArenaArt.Cell("GuardianMotion-"+Names[hero]+"-v1",6,3,frame,paint);
            return Frames[hero,paint,frame];
        }
        public static Sprite Sample(int hero,int skin,int outfit,float time,float speed,bool firing,bool hurt,bool ultimate)
        {
            if(Outfits.Valid(outfit))
                return CharacterArt.Get(hero,skin,outfit,ultimate?3:hurt?0:firing?2:speed>.1f&&(int)(time*8)%2==1?1:0);
            int frame=hurt?5:firing?4:speed>.1f?(int)(time*8)%4:-1;
            return frame<0?CharacterArt.Get(hero,skin):Frame(hero,skin,frame)??CharacterArt.Get(hero,skin);
        }
        // Same continuous motion in battle and fitting room. Uses the caller's clock so pauses freeze it.
        public static Vector3 PoseScale(float time,float moving,bool firing,bool hurt)
        {
            float stride=Mathf.Sin(time*16)*Mathf.Clamp01(moving);
            float breathe=Mathf.Sin(time*2.8f)*.008f*(1-Mathf.Clamp01(moving));
            return new Vector3(1+stride*.025f+(hurt?.035f:0),1-stride*.025f+breathe-(firing?.025f:0),1);
        }
    }
}
