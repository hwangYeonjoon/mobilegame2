using UnityEngine;
namespace ScrapGuard
{
    // CC0 Kenney recordings. See ArtSource/Audio/LICENSES.md for source and hashes.
    public static class CombatSoundBank
    {
        static readonly string[] names={"Hit","Break","Pickup","Growth","Shot","Heavy","Shield","Arc","Frost","Ultimate","Repair","Assembly"};
        static readonly AudioClip[] clips=new AudioClip[12];
        public static AudioClip Get(int id)
        {id=Mathf.Clamp(id,0,11);if(clips[id]==null)clips[id]=Resources.Load<AudioClip>("Audio/Licensed/"+names[id]);return clips[id];}
    }
}
