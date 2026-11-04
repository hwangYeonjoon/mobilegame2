namespace ScrapGuard.Core
{
    public static class Outfits
    {
        public const string Key = "outfit_heavy_engineer", Name = "중장비 정비사";
        public const int Price = 600;
        public const string RailKey="outfit_rail_ranger", RailName="레일 레인저";
        // Append only: persisted purchases, public profiles and guild supports share these IDs.
        public const int Count=10, LatestCollection=2;
        public static readonly string[] Collections={"공방의 전설","새 지평선","극한 작업반"};
        public static readonly string[] Keys={"",Key,RailKey,"outfit_storm_commander","outfit_aurora_keeper","outfit_citadel_sentinel","outfit_orbital_mechanic","outfit_neon_tracker","outfit_solar_technician","outfit_abyss_explorer","outfit_deep_miner"};
        public static readonly string[] Labels={"기본 외형",Name,RailName,"폭풍 지휘자","오로라 수호자","성채 파수꾼","궤도 정비사","네온 추적자","태양로 기술자","심해 탐사자","심층 채굴자"};
        public static readonly string[] Assets={"","Luke-HeavyEngineer-v1","Ray-RailRanger-v1","Spark-StormCommander-v1","Iris-AuroraKeeper-v1","Volt-CitadelSentinel-v1","Luke-OrbitalMechanic-v1","Ray-NeonTracker-v1","Spark-SolarTechnician-v1","Iris-AbyssExplorer-v1","Volt-DeepMiner-v1"};
        public static readonly string[] Details={"","중장갑 · 리벳 대포 · 크레인 전개","철도 장갑 · 레일건 · 붉은 스카프","황금 장갑 · 쌍둥이 코일 · 전격 날개","수정 왕관 · 눈꽃 방패 · 결정 망토","성벽 장갑 · 육각 방패 · 수호 깃발","우주 장갑 · 궤도 고리 · 위성 패널","질주 장갑 · 갈래 광선총 · 네온 스카프","태양 장갑 · 회전 터빈 · 방열 핀","잠수 장갑 · 음파 탐지기 · 산소 탱크","암반 장갑 · 나선 드릴 · 굴착 지지대"};
        public static readonly string[] Effects={"기본 사격","리벳 탄환 · 기어 충격파","쌍선 탄환 · 레일 섬광","전격 탄환 · 번개 날개","결정 탄환 · 눈꽃 결정","육각 탄환 · 성문 전개","고리 탄환 · 위성 공전","갈래 탄환 · 네온 질주","태양 탄환 · 코로나 회전","음파 탄환 · 심해 파동","드릴 탄환 · 굴착 전개"};
        public static readonly string[] Arrivals={"지원 도착","공방 가동","레일 접속","폭풍 전개","오로라 개방","성문 개방","궤도 진입","신호 포착","태양로 점화","심해 부상","지층 돌파"};
        static readonly int[] Heroes={0,0,1,3,4,2,0,1,3,4,2};
        public static int Hero(int id){return Valid(id)?Heroes[id]:0;}
        public static int Collection(int id){return Valid(id)?(id>=8?2:id>=5?1:0):0;}
        public static string HeroName(int id){return HeroCatalog.All[Hero(id)].Name.Split(' ')[1];}
        public static string Label(int id){return Valid(id)?Labels[id]:Labels[0];}
        public static string Product(int id){return Valid(id)?Keys[id]:"";}
        public static int Find(string key){for(int i=1;i<=Count;i++)if(Keys[i]==key)return i;return 0;}
        public static bool Valid(int id){return id>=1 && id<=Count;}
        public static bool Owned(GameState s,int id=1) { return Valid(id) && s.ownedCosmetics != null && s.ownedCosmetics.Contains(Product(id)); }
        public static void Normalize(GameState s)
        {
            if(s.heroOutfits==null || s.heroOutfits.Count==0)
            {
                s.heroOutfits=new System.Collections.Generic.List<int>(new int[HeroCatalog.Count]);
                if(Valid(s.selectedOutfit) && Owned(s,s.selectedOutfit))s.heroOutfits[Hero(s.selectedOutfit)]=s.selectedOutfit;
            }
            while(s.heroOutfits.Count<HeroCatalog.Count)s.heroOutfits.Add(0);
            if(s.heroOutfits.Count>HeroCatalog.Count)s.heroOutfits.RemoveRange(HeroCatalog.Count,s.heroOutfits.Count-HeroCatalog.Count);
            for(int h=0;h<HeroCatalog.Count;h++)if(!Owned(s,s.heroOutfits[h]) || Hero(s.heroOutfits[h])!=h || !s.heroes[h].unlocked)s.heroOutfits[h]=0;
            if(!Owned(s,s.selectedOutfit))s.selectedOutfit=0;
            if(!Valid(s.outfitTarget) || Owned(s,s.outfitTarget))s.outfitTarget=0;
        }
        public static int Active(GameState s, int hero)
        {
            if(!HeroCatalog.Valid(hero))return 0;
            int id=s.heroOutfits!=null && s.heroOutfits.Count>hero?s.heroOutfits[hero]:s.selectedOutfit;
            return Valid(id) && Hero(id)==hero && Owned(s,id) && s.heroes[hero].unlocked?id:0;
        }
        public static CommandResult Equip(GameState s, int id)
        {
            if(id!=0 && (!Owned(s,id) || !s.heroes[Hero(id)].unlocked))return CommandResult.Fail("의상과 전용 대원을 보유해야 장착할 수 있어요.");
            Normalize(s);
            // Legacy clear command retains its all-outfits meaning. New UI clears one hero explicitly.
            if(id==0)for(int h=0;h<s.heroOutfits.Count;h++)s.heroOutfits[h]=0;
            else s.heroOutfits[Hero(id)]=id;
            s.selectedOutfit=id;
            return CommandResult.Ok(id==0?"의상을 해제하고 기존 도색을 표시해요.":Label(id)+" 장착 · 다른 대원의 의상은 유지돼요.");
        }
        public static CommandResult Clear(GameState s,int hero)
        {
            if(!HeroCatalog.Valid(hero))return CommandResult.Fail("없는 대원이에요.");
            Normalize(s);s.heroOutfits[hero]=0;if(Valid(s.selectedOutfit)&&Hero(s.selectedOutfit)==hero)s.selectedOutfit=0;
            return CommandResult.Ok("이 대원의 의상을 해제했어요. 다른 대원은 그대로예요.");
        }
        public static CommandResult Target(GameState s,int id)
        {
            if(id!=0 && (!Valid(id)||Owned(s,id)))return CommandResult.Fail("아직 보유하지 않은 의상을 선택하세요.");
            s.outfitTarget=id;return CommandResult.Ok(id==0?"의상 목표를 해제했어요.":Label(id)+" 수집 목표 저장 · 자동 구매되지 않아요.");
        }

    }

    public static class StarterGuide
    {
        public static readonly string[] Names = { "이동하며 생존하기", "필살기 사용하기", "첫 장비 강화", "첫 개조 완성", "첫 지역 정복" };
        public static readonly string[] Details = { "전투 화면을 드래그해 이동하세요. 공격은 자동이에요.", "적을 처치해 충전하고 필살기 버튼을 누르세요.", "장비에서 무기를 골라 골드와 부품으로 강화하세요.", "현장 임무 완료 후 개조를 장착하거나 스킬을 조합하세요.", "작전 6의 지역 보스를 처치하고 다음 지역으로 가세요." };
        public static bool Done(GameState s, int step)
        {
            switch(step) { case 0:return s.guideMoved; case 1:return s.guideUltimate; case 2:return s.guideUpgrade; case 3:return s.guideMod; case 4:return s.clearedStages.Contains(5); default:return true; }
        }
        public static int Next(GameState s) { for(int i=0;i<Names.Length;i++)if(!Done(s,i))return i;return Names.Length; }
        public static void Normalize(GameState s)
        {
            if(s.guideVersion>0)return;
            s.guideVersion=1;
            // Older experienced accounts keep their progress and are not forced through an introduction.
            s.guideIntroSeen=s.totalRuns>0 || s.tutorialCompleted;
            s.guideUpgrade=s.items.Exists(i=>i.level>1);
            s.guideMod=s.receipts.Exists(r=>r.report!=null && r.report.mods!=0);
        }
        public static void Record(GameState s, Combat combat)
        {
            s.guideMoved |= combat.DistanceMoved >= 120;
            s.guideUltimate |= combat.UltimateUses > 0;
            s.guideMod |= combat.Mods != 0;
        }
    }
}
