using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        readonly Dictionary<long,List<Enemy>> enemyGrid=new Dictionary<long,List<Enemy>>();
        readonly List<Enemy> nearbyBullets=new List<Enemy>(64);
        static long CellKey(int x,int y){return ((long)x<<32)|(uint)y;}
        void IndexEnemies()
        {
            foreach(var cell in enemyGrid.Values)cell.Clear();
            foreach(var e in Enemies)
            {
                if(e.hp<=0)continue;
                long key=CellKey((int)Math.Floor(e.position.x/80),(int)Math.Floor(e.position.y/80));
                if(!enemyGrid.TryGetValue(key,out var cell)){cell=new List<Enemy>();enemyGrid[key]=cell;}
                cell.Add(e);
            }
        }
        List<Enemy> BulletCandidates(V2 position)
        {
            nearbyBullets.Clear();
            int x=(int)Math.Floor(position.x/80),y=(int)Math.Floor(position.y/80);
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                if(enemyGrid.TryGetValue(CellKey(x+dx,y+dy),out var cell))nearbyBullets.AddRange(cell);
            // Preserve original list/ID order: piercing and blast results are deterministic.
            nearbyBullets.Sort((a,b)=>a.id.CompareTo(b.id));return nearbyBullets;
        }
    }
}
