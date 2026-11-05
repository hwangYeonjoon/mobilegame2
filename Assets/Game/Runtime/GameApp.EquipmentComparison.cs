using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void EquipmentComparison(Transform parent,string offer,float y)
        {
            int definition=int.Parse(offer.Split(':')[1]);
            var next=new Item{definition=definition,rarity=offer.StartsWith("rare:")?2:0,level=1};
            int slot=Catalog.Items[definition].Slot;
            var current=State.items.Find(i=>i.id==State.equipped[slot]);
            var panel=Ui.Box(parent,24,y,444,269,Ui.Background).transform;
            Item[] items={current,next};
            for(int i=0;i<2;i++)
            {
                var item=items[i];float x=12+i*222;
                Ui.Label(panel,i==0?"현재 장착":"구매할 장비",x,8,208,27,18,Ui.Muted);
                ItemIcon(panel,item,x,43,64);
                Ui.Label(panel,Catalog.Items[item.definition].Name,x+74,40,130,32,18);
                Ui.Label(panel,Catalog.RarityNames[item.rarity]+" Lv."+item.level,x+74,77,130,28,17,Rarity(item.rarity));
            }
            int level=State.heroes[State.selectedHero].level;
            string delta=slot==0?"공격 "+Catalog.AttackFor(State.selectedHero,level,current)+" → "+Catalog.AttackFor(State.selectedHero,level,next):
                slot==1?"체력 "+Catalog.HealthFor(State.selectedHero,level,current)+" → "+Catalog.HealthFor(State.selectedHero,level,next):"모듈 교체 · 아래 효과로 변경";
            Ui.Label(panel,delta,12,119,420,32,22,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(panel,"현재: "+Catalog.Items[current.definition].Description+"\n변경: "+Catalog.Items[definition].Description,12,157,420,68,18,Ui.Ink,TextAnchor.MiddleLeft);
            int count=State.items.FindAll(i=>i.definition==definition&&i.rarity==next.rarity).Count;
            Ui.Label(panel,"같은 등급 보유 "+count+"개 · Lv.1 지급 · 자동 장착 안 함",12,235,420,27,16,Ui.Muted,TextAnchor.MiddleCenter);
        }
    }
}
