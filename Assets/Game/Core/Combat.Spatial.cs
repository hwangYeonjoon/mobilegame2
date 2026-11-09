using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        readonly Dictionary<long,List<Enemy>> enemyGrid=new Dictionary<long,List<Enemy>>();
        readonly Dictionary<long,List<Enemy>> candidateCache=new Dictionary<long,List<Enemy>>();
        readonly HashSet<long> builtCandidates=new HashSet<long>();
        readonly List<Enemy>[] mergeCells=new List<Enemy>[9];
        readonly int[] mergeAt=new int[9];
        V2[] separation=new V2[512];
        public int CandidateBuilds {get;private set;}
        public int SeparationPairs {get;private set;}
        static long CellKey(int x,int y){return ((long)x<<32)|(uint)y;}
        void IndexEnemies()
        {
            foreach(var cell in enemyGrid.Values)cell.Clear();
            builtCandidates.Clear();CandidateBuilds=0;
            for(int i=0;i<Enemies.Count;i++)
            {
                var e=Enemies[i];e.spatialIndex=i;if(e.hp<=0)continue;
                long key=CellKey((int)Math.Floor(e.position.x/80),(int)Math.Floor(e.position.y/80));
                if(!enemyGrid.TryGetValue(key,out var cell)){cell=new List<Enemy>();enemyGrid[key]=cell;}
                bool unordered=cell.Count>0&&cell[cell.Count-1].id>e.id;cell.Add(e);if(unordered)cell.Sort((a,b)=>a.id.CompareTo(b.id));
            }
        }
        List<Enemy> BulletCandidates(V2 position)
        {
            int x=(int)Math.Floor(position.x/80),y=(int)Math.Floor(position.y/80);long key=CellKey(x,y);
            if(!candidateCache.TryGetValue(key,out var result)){result=new List<Enemy>(64);candidateCache[key]=result;}
            if(!builtCandidates.Add(key))return result;
            result.Clear();CandidateBuilds++;int cells=0;
            // Cell lists retain monotonic spawn IDs. Merge once per occupied projectile cell,
            // reuse for all bullets there; no per-bullet sort and no gameplay RNG.
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                if(enemyGrid.TryGetValue(CellKey(x+dx,y+dy),out var cell)&&cell.Count>0){mergeCells[cells]=cell;mergeAt[cells++]=0;}
            while(true)
            {
                int best=-1,id=int.MaxValue;
                for(int i=0;i<cells;i++)if(mergeAt[i]<mergeCells[i].Count&&mergeCells[i][mergeAt[i]].id<id){best=i;id=mergeCells[i][mergeAt[i]].id;}
                if(best<0)break;result.Add(mergeCells[best][mergeAt[best]++]);
            }
            return result;
        }
        void SeparateEnemies(float dt)
        {
            IndexEnemies();SeparationPairs=0;
            if(separation.Length<Enemies.Count)Array.Resize(ref separation,Enemies.Count*2);
            Array.Clear(separation,0,Enemies.Count);
            foreach(var e in Enemies)
            {
                if(e.hp<=0||e.core)continue;
                int x=(int)Math.Floor(e.position.x/80),y=(int)Math.Floor(e.position.y/80);
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                {
                    if(!enemyGrid.TryGetValue(CellKey(x+dx,y+dy),out var cell))continue;
                    foreach(var other in cell)
                    {
                        if(other.id<=e.id||other.hp<=0||other.core)continue;
                        SeparationPairs++;var delta=e.position-other.position;double gap=(e.Radius+other.Radius)*.88;
                        if(Math.Abs(delta.x)>=gap||Math.Abs(delta.y)>=gap)continue;
                        float distance=delta.Length;if(distance>=gap)continue;
                        var direction=distance>.01f?delta/distance:V2.Angle(((e.id*17+other.id*31)&7)*Math.PI/4);
                        // Keep subtraction and scaling in double until the position grid is rounded.
                        var push=direction*((gap-distance)*.32);
                        separation[e.spatialIndex]+=push;separation[other.spatialIndex]-=push;
                    }
                }
            }
            foreach(var e in Enemies)
            {
                if(e.core||e.hp<=0)continue;var push=separation[e.spatialIndex];float length=push.Length;
                if(length>.001f)e.position=ClampWorld(Slide(e.position,push/length*Math.Min(length,55*dt),Math.Min(e.Radius,20)),20);
            }
        }
    }
}
