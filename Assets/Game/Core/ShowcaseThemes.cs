namespace ScrapGuard.Core
{
    public static class ShowcaseThemes
    {
        public static readonly string[] Names={"기본 정비대","궤도 관측소","심해 정비항","정비소 탈환 기념관","극저온 정복 기념관"};
        public static readonly string[] Keys={"","theme_orbit","theme_abyss","",""};
        public const int Price=400;
        public static bool Valid(int id){return id>=0&&id<Names.Length;}
        public static int Find(string key){return key==Keys[1]?1:key==Keys[2]?2:-1;}
        public static bool Owned(GameState s,int id){return id==0||id==1||id==2?(id==0||s.ownedCosmetics!=null&&s.ownedCosmetics.Contains(Keys[id])):id==3?s.clearedStages.Contains(5):id==4&&s.clearedStages.Contains(23);}
        public static CommandResult Equip(GameState s,int id)
        {
            if(!Valid(id)||!Owned(s,id))return CommandResult.Fail("아직 획득하지 않은 전시 배경이에요.");
            s.showcaseTheme=id;return CommandResult.Ok(Names[id]+" 적용 · 능력치 변화 없음");
        }
        public static string Goal(int id){return id==0?"기본 제공":id<=2?"보석 400 · 모든 대원 공용":id==3?"작전 06 완료로 획득":"작전 24 완료로 획득";}
    }
}
