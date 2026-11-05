using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        float menuExtra;
        bool menuLayoutDirty;
        float MenuHeight { get { return 960+menuExtra; } }
        float MenuBottom { get { return MenuHeight-116; } }
        void FitMenu(float scale)
        {
            float extra=Mathf.Max(0,Screen.safeArea.height/scale-960);
            if(Mathf.Abs(extra-menuExtra)>1){menuExtra=extra;menuLayoutDirty=true;}
            if(screen!=null)
            {
                screen.anchoredPosition=new Vector2(0,combat==null?menuExtra/2:0);
                screen.sizeDelta=new Vector2(540,combat==null?MenuHeight:960);
            }
        }
    }
}
