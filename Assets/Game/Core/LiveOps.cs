using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    [Serializable] public class OutfitRelease
    {
        public int id,minimumBuild;public bool paused;public string startsAt,endsAt;
        public bool Available(DateTimeOffset now,int build)
        {
            DateTimeOffset starts,ends;
            return !paused&&build>=minimumBuild&&(string.IsNullOrEmpty(startsAt)||DateTimeOffset.TryParse(startsAt,out starts)&&now>=starts)&&(string.IsNullOrEmpty(endsAt)||DateTimeOffset.TryParse(endsAt,out ends)&&now<ends);
        }
    }
    [Serializable] public class ContentCatalog { public string serverTime;public List<OutfitRelease> outfits=new List<OutfitRelease>(); }
}
