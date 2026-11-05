using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public static class InventoryArt
    {
        public static Color Rarity(int tier) { return tier==2?Ui.Blue:tier==1?Ui.Mint:Ui.Muted; }
        public static string Rank(int tier) { return new[]{"I","II","III"}[Mathf.Clamp(tier,0,2)]; }
        public static string Slot(int slot) { return new[]{"무기","갑옷","모듈"}[Mathf.Clamp(slot,0,2)]; }
        public static Sprite Equipment(int id) { return ArenaArt.Cell("EquipmentIcons-v1",4,3,id%4,id/4); }
        public static Sprite Skill(int id) { return ArenaArt.Cell("SkillIcons-v1",4,2,id%4,id/4); }
        static Texture2D packAtlas;
        static readonly Sprite[] packs=new Sprite[5];
        public static Sprite Pack(int id)
        {
            if(id<0||id>=packs.Length)return null;
            if(packs[id]!=null)return packs[id];
            if(packAtlas==null)packAtlas=Resources.Load<Texture2D>("Art/ShopPacks-v1");
            if(packAtlas==null)return null;
            float width=packAtlas.width/5f;
            // Trim measured transparent top/bottom gutters without changing the source artwork.
            packs[id]=Sprite.Create(packAtlas,new Rect(id*width+1,packAtlas.height*.16f,width-2,packAtlas.height*.68f),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            return packs[id];
        }
        public static Image Draw(Transform parent,Sprite sprite,float x,float y,float size)
        {
            var image=Ui.Rect(parent,"Inventory art",x,y,size,size).gameObject.AddComponent<Image>();
            image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        public static void Item(Transform parent,Item item,float x,float y,float size)
        {
            var frame=Ui.Box(parent,x,y,size,size,Rarity(item.rarity)).transform;
            Ui.Box(frame,2,2,size-4,size-4,Ui.Background);
            Draw(frame,Equipment(item.definition),4,4,size-8);
            Ui.Box(frame,size-25,size-21,23,19,Ui.Background);
            Ui.Label(frame,Rank(item.rarity),size-24,size-21,22,19,12,Rarity(item.rarity),TextAnchor.MiddleCenter);
        }
    }

    // Decorative room only: no world collision or gameplay state.
    public static class WorkshopStage
    {
        public static RectTransform Draw(Transform parent,float x,float y,float width,float height,Color accent)
        {
            var root=Ui.Box(parent,x,y,width,height,Ui.Hex("142632"),true,"Workshop display").rectTransform;
            root.gameObject.AddComponent<RectMask2D>();
            for(int i=0;i<5;i++)Ui.Box(root,12,16+i*(height-36)/5,width-24,1,new Color(.45f,.65f,.7f,.10f),false);
            for(int side=0;side<2;side++)
            {
                float sx=side==0?14:width-34;
                Ui.Box(root,sx,12,20,height-24,Ui.Hex("223A45"));
                Ui.Box(root,sx+7,25,6,height*.42f,new Color(accent.r,accent.g,accent.b,.38f));
                var lamp=Ui.Rect(root,"Warm workshop light",side==0?0:width-150,12,150,height*.65f).gameObject.AddComponent<AmbientLight>();
                lamp.color=new Color(accent.r,accent.g,accent.b,.15f);lamp.raycastTarget=false;
                InventoryArt.Draw(root,InventoryArt.Equipment(side==0?1:10),sx-6,height*.61f,38).color=new Color(.7f,.8f,.8f,.48f);
            }
            var shadow=Ui.Disc(root,width*.18f,height-53,width*.64f,new Color(0,0,0,.35f));shadow.rectTransform.sizeDelta=new Vector2(width*.64f,43);
            var plinth=Ui.Disc(root,width*.22f,height-59,width*.56f,Ui.Hex("344D57"));plinth.rectTransform.sizeDelta=new Vector2(width*.56f,32);
            var rim=Ui.Disc(root,width*.24f,height-56,width*.52f,new Color(accent.r,accent.g,accent.b,.24f));rim.rectTransform.sizeDelta=new Vector2(width*.52f,21);
            Ui.Box(root,width*.39f,height-24,width*.22f,3,accent);
            return root;
        }
    }
}
