using System;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
using Social=ScrapGuard.Core.Social;

namespace ScrapGuard
{
    public partial class GameApp
    {
        RectTransform turretArt, twinTurretArt, magnetField;
        void ModBook()
        {
            var card=Dialog("고철 개조 도감","전투 중 두 스킬을 조합하면 자동으로 개조됩니다.\n개조는 이번 전투에만 적용돼요.",820);
            var list=Ui.Scroll(card,24,140,444,580,(Workshop.ModNames.Length+6)*205);
            for(int i=0;i<Workshop.ModNames.Length;i++)
            {
                var row=Ui.Box(list,0,i*205,444,191,Ui.Background).transform;
                Ui.Label(row,Workshop.ModNames[i],16,8,412,34,24,Ui.Mint);
                InventoryArt.Draw(row,InventoryArt.Skill(Workshop.MainSkills[i]),12,49,52);
                InventoryArt.Draw(row,InventoryArt.Skill(Workshop.SubSkills[i]),232,49,52);
                Ui.Label(row,Catalog.SkillNames[Workshop.MainSkills[i]]+"\nLv."+Workshop.MainLevels[i],70,48,142,55,18,Ui.Gold);
                Ui.Label(row,"+",210,54,22,42,23,Ui.Muted);
                Ui.Label(row,Catalog.SkillNames[Workshop.SubSkills[i]]+"\nLv."+Workshop.SubLevels[i],290,48,142,55,18,Ui.Gold);
                Ui.Label(row,Workshop.ModDetails[i],16,117,412,62,19,Ui.Muted);
            }
            int rowIndex=Workshop.ModNames.Length;
            foreach(int skill in new[]{1,2,3,8,9,10})
            {
                var row=Ui.Box(list,0,rowIndex++*205,444,191,Ui.Background).transform;
                InventoryArt.Draw(row,InventoryArt.Skill(skill),16,18,60);
                Ui.Label(row,Combat.EvolutionTitle(skill),90,22,338,38,25,Ui.Mint);
                Ui.Label(row,Combat.EvolutionRecipe(skill),16,92,412,72,20,Ui.Ink);
            }
            Ui.Button(card,"현장 개조 · 대원 전용 효과",24,740,444,60,FieldEngineeringGuide,Ui.Mint,true,20);
        }
        string ModHint(int skill)
        {
            string recipe=Combat.EvolutionRecipe(skill,combat.Challenge);if(recipe.Length>0)return Combat.EvolutionTitle(skill)+"\n"+recipe;
            string hints="";int shown=0;
            for(int i=0;i<Workshop.ModNames.Length && shown<2;i++)if(Workshop.MainSkills[i]==skill || Workshop.SubSkills[i]==skill)
            {
                int main=combat.Skills[Workshop.MainSkills[i]]+(Workshop.MainSkills[i]==skill?1:0);
                int sub=combat.Skills[Workshop.SubSkills[i]]+(Workshop.SubSkills[i]==skill?1:0);
                hints+="\n"+Workshop.ModNames[i]+(main>=Workshop.MainLevels[i] && sub>=Workshop.SubLevels[i]?" 완성!":" · "+Math.Min(main,Workshop.MainLevels[i])+"/"+Workshop.MainLevels[i]+" + "+Math.Min(sub,Workshop.SubLevels[i])+"/"+Workshop.SubLevels[i]);shown++;
            }
            if((skill==0||skill==4||skill==5)&&combat.Skills[0]>=3)return "리벳 5 + 고밀도 3 → 중장\n리벳 5 + 장전 3 → 분산 (먼저 완성한 쪽)";
            return hints;
        }
        void RegionCollection(int region)
        {
            Workshop.Normalize(State);var progress=Workshop.Region(State,region);int skin=region+3;bool owned=Social.HasSkin(State,skin);
            var card=Dialog("지역 수집 · 전용 도색","작전 완료마다 확정 설계도 · 뽑기 없음",850);
            Ui.Button(card,"이전",24,128,86,42,()=>RegionCollection(region-1),Ui.Raised,region>0,17);
            Ui.Label(card,(region+1)+" / "+Campaign.Regions.Length+" 지역",120,128,250,42,20,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,"다음",382,128,86,42,()=>RegionCollection(region+1),Ui.Raised,region<Campaign.Regions.Length-1,17);
            Ui.Label(card,Campaign.Regions[region],24,187,444,39,25);
            CharacterArt.Draw(card,35,247,144,155,State.showcaseHero,skin);
            Ui.Label(card,Social.Skins[skin],195,251,270,38,23,CharacterArt.Tint(skin));
            Ui.Label(card,"전체 대원 공용 도색\n능력치 변화 없음\n이 지역 작전에서만 설계도 획득",195,299,270,93,16,Ui.Muted);
            Ui.Label(card,"설계도 "+progress.blueprints+" / "+Workshop.BlueprintCost+"     완료 "+progress.wins+"회",24,420,444,37,21,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,"성공 1개 · 지역 마지막 작전 성공 2개\n"+(owned?"도색 제작 완료": "제작까지 "+Math.Max(0,Workshop.BlueprintCost-progress.blueprints)+"개 남았어요."),24,465,444,54,16,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,owned?State.selectedSkin==skin?"사용 중인 도색":"내 대원에 적용":"설계도 8개로 제작",24,539,444,57,()=>
            {
                if(owned)Change(new GameCommand("customize"){hero=State.showcaseHero,title=State.showcaseTitle,frame=State.showcaseFrame,skin=skin},()=>RegionCollection(region));
                else ConfirmRegionCraft(region);
            },null,owned || progress.blueprints>=Workshop.BlueprintCost,19);
            Ui.Button(card,"적 · 보스 도감",24,616,214,51,EnemyCodex,Ui.Raised,true,16);
            Ui.Button(card,"개조 조합 도감",254,616,214,51,ModBook,Ui.Raised,true,16);
            Ui.Button(card,State.collectionTarget==region?"수집 목표 해제":"수집 목표로 등록",24,690,214,51,()=>Change(new GameCommand("region_target"){stage=State.collectionTarget==region?-1:region},()=>RegionCollection(region)),Ui.Mint,true,16);
            Ui.Button(card,"이 지역 작전 보기",254,690,214,51,()=>CampaignMap(region),Ui.Raised,true,16);
            Ui.Label(card,"기본 장갑에 지역 색을 적용한 도색입니다.\n정비소·전투·전시·공개한 길드 지원에 적용됩니다.",24,762,444,57,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void ConfirmRegionCraft(int region)
        {
            var card=Dialog("지역 도색 제작",Social.Skins[region+3]+" · 설계도 8개 사용",420);
            Ui.Label(card,"이 지역에서 모은 설계도로 영구 도색을 제작해요.\n능력치는 바뀌지 않으며 모든 보유 대원에게 적용됩니다.",24,142,444,82,17,Ui.Muted);
            Ui.Button(card,"제작 확정",24,257,444,57,()=>Change(new GameCommand("region_craft"){stage=region},()=>RegionCollection(region)),Ui.Gold,true,19);
            Ui.Button(card,"돌아가기",24,333,444,43,()=>RegionCollection(region),Ui.Raised,true,16);
        }
        void EnemyCodex()
        {
            var card=Dialog("기계 생태 도감","처치한 적과 토벌한 지역 보스가 기록됩니다.",870);
            var list=Ui.Scroll(card,24,136,444,695,(6+Campaign.Regions.Length)*107);
            string[] names={"고철 집게","돌진 드릴","포격 궤도차","자폭 반응로","방패 보행기","소환 송신기"};
            string[] tips={"접촉 전에 거리를 확보하세요","돌진 준비 후 옆으로 회피","포격 예고 원을 벗어나세요","자폭 준비 중에는 거리를 벌리세요","가까이 접근하면 방패 감쇄가 해제돼요","소환기를 먼저 처리하세요"};
            for(int i=0;i<6+Campaign.Regions.Length;i++)
            {
                bool boss=i>=6;int index=boss?i-6:i;bool discovered=((boss?State.bossDiscoveries:State.enemyDiscoveries)&(1<<index))!=0;float y=i*107;
                var row=Ui.Box(list,0,y,444,97,Ui.Background).transform;
                var art=Ui.Box(row,8,7,82,82,discovered?Color.white:new Color(.2f,.24f,.27f,1),false);art.sprite=BattleArt.Role(index,0,boss);art.preserveAspect=true;
                Ui.Label(row,boss?Campaign.FinalStage(index).Boss:names[index],104,9,324,29,20,discovered?Ui.Ink:Ui.Muted);
                Ui.Label(row,discovered?(boss?Campaign.Regions[index]+" · 토벌 기록 보유":tips[index]):"미발견 · 전투에서 처치하면 기록",104,45,322,43,14,Ui.Muted);
            }
        }
        void BattleAnalysis()
        {
            var receipt=State.receipts.Find(r=>r.id==combat.RunId);var report=receipt==null?combat.Report():receipt.report;
            if(report==null){Info("전투 분석","이전 버전 기록에는 상세 분석이 없어요.");return;}
            var card=Dialog("이번 전투 분석",combat.Challenge?FieldEngineering.TrialTitle(combat.Mode):Catalog.StageNames[combat.Stage],850);
            int cause=Workshop.MainCause(report);
            Ui.Label(card,(report.cause>0?"종료 원인  ":"주요 피해  ")+Workshop.CauseNames[cause],24,128,444,40,24,Ui.Gold);
            Ui.Label(card,BossTraining.Advice(report),24,184,444,69,18);
            Ui.Label(card,report.bossRemaining<0?"보스 잔여 체력: 미등장 또는 해당 없음":"보스 잔여 체력  "+Mathf.CeilToInt(report.bossRemaining*100)+"%",24,269,444,35,18,Ui.Mint);
            string damage="";
            if(report.recentHits!=null&&report.recentHits.Count>0)
            {
                var totals=new float[6];foreach(var hit in report.recentHits)if(hit.source>=0&&hit.source<totals.Length)totals[hit.source]+=hit.damage;
                damage="최근 8초 · 피해 출처\n";for(int i=0;i<totals.Length;i++)if(totals[i]>0)damage+=CombatTraining.SourceName(i)+"  "+Mathf.CeilToInt(totals[i])+"\n";
                damage+="\n전투 전체 · 피해 종류\n";
            }
            for(int i=1;i<Math.Min(Workshop.CauseNames.Length,report.damage.Length);i++)if(report.damage[i]>0)damage+=Workshop.CauseNames[i]+"  "+Mathf.CeilToInt(report.damage[i])+"\n";
            Ui.Label(card,"마지막 피격 · "+CombatTraining.SourceName(report.lastSource)+" · −"+Mathf.CeilToInt(report.lastHitDamage),24,325,444,28,15,Ui.Muted);
            var list=Ui.Scroll(card,24,363,444,147,Math.Max(147,damage.Split('\n').Length*29));Ui.Label(list,damage.Length==0?"체력 피해 없음":damage,12,0,420,Math.Max(147,damage.Split('\n').Length*29),17);
            string mods=report.fieldModule>0?FieldEngineering.Name(report.fieldModule,report.hero)+" (현장 장착)\n":"";for(int i=0;i<Workshop.ModNames.Length;i++)if((report.mods&(1<<i))!=0)mods+=Workshop.ModNames[i]+"\n";
            for(int i=0;i<12;i++)if((report.skillEvolutions&(1<<i))!=0)mods+=Combat.EvolutionTitle(i)+"\n";
            var builds=Ui.Scroll(card,24,534,444,101,Mathf.Max(101,(mods.Split('\n').Length+1)*23));Ui.Label(builds,"개조 · 진화\n"+(mods.Length==0?"없음 · 도감에서 조건을 확인하세요.":mods),0,0,424,Mathf.Max(101,(mods.Split('\n').Length+1)*23),14,Ui.Mint);
            string skills="";for(int i=0;i<report.skills.Length;i++)if(report.skills[i]>0)skills+=(skills.Length>0?" · ":"")+Catalog.SkillNames[i]+" "+report.skills[i];
            Ui.Label(card,skills,24,643,444,68,13,Ui.Muted);
            Ui.Label(card,"고철 합체 "+report.machines+"회 · 웨이브 "+report.waves+" · "+(report.evolution==0?"진화 미완성":report.evolution==1?"중장 진화":"분산 진화")+(report.revived?" · 긴급 수리 사용":""),24,716,444,30,13,Ui.Mint);
            Ui.Button(card,"결과로",24,760,214,53,ShowResult,Ui.Raised,true,18);
            int practiceStage=combat.Stage;
            Ui.Button(card,Campaign.Get(practiceStage).Mission==Mission.Survive?"이 지역 무료 연습":"이 보스 무료 연습",254,760,214,53,()=>{combat=null;StartBattle(practiceStage,false,CombatTraining.RetryMode(State,practiceStage));},Ui.Mint,!combat.Challenge&&CombatTraining.Available(State,practiceStage),17);
        }
        void RenderModEffects()
        {
            if(magnetField==null)magnetField=CombatRing.Create(effectLayer,300,new Color(.35f,1,.7f,.18f),.009f);
            magnetField.gameObject.SetActive(combat.HasMod(0));magnetField.anchoredPosition=new Vector2(combat.Position.x,-combat.Position.y);
            if(turretArt==null)
            {
                turretArt=Ui.Rect(effectLayer,"Rivet sentry",0,0,28,28);turretArt.pivot=new Vector2(.5f,.5f);
                Ui.Disc(turretArt,0,0,28,Ui.Hex("253344"));
                Ui.Disc(turretArt,3,3,22,Ui.Hex("AC8ADE"));
                Ui.Disc(turretArt,7,7,14,Ui.Hex("3A3358"));
                Ui.Box(turretArt,6,-8,5,20,Ui.Hex("BDD3E6"),false);
                Ui.Box(turretArt,17,-8,5,20,Ui.Hex("BDD3E6"),false);
                Ui.Box(turretArt,7,-8,3,5,Ui.Hex("D8BAFF"),false);
                Ui.Box(turretArt,18,-8,3,5,Ui.Hex("D8BAFF"),false);
                Ui.Disc(turretArt,10,10,8,Ui.Hex("E8C6FF"));
            }
            if(twinTurretArt==null){twinTurretArt=Instantiate(turretArt,effectLayer);twinTurretArt.name="Twin sentry";}
            twinTurretArt.gameObject.SetActive(combat.FieldModule==1 && combat.Hero==0);
            var twin=combat.TurretPosition+new V2(40,0);
            twinTurretArt.anchoredPosition=new Vector2(twin.x,-twin.y);
            twinTurretArt.localEulerAngles=new Vector3(0,0,-combat.Facing*Mathf.Rad2Deg);
            turretArt.gameObject.SetActive(combat.HasMod(2));
            turretArt.anchoredPosition=new Vector2(combat.TurretPosition.x,-combat.TurretPosition.y);
            turretArt.localEulerAngles=new Vector3(0,0,-combat.Facing*Mathf.Rad2Deg);
        }
        void WeeklyRecordCard(){WeeklyRecordCard(false);}
        void WeeklyRecordCard(bool skill)
        {
            string week=CurrentWeek();var progress=skill?State.skillWeekly.Find(w=>w.week==week):Endgame.Find(State,week);int score=progress==null?0:progress.bestScore;
            var modalCard=Dialog("주간 기록 카드","이번 주에 저장된 개인 최고 기록을 공유하세요.",850);
            var plate=Ui.Box(modalCard,24,139,444,550,Ui.Background).rectTransform;
            Ui.Box(plate,0,0,444,5,FrameColor(State.showcaseFrame),false);
            Ui.Label(plate,skill?"고철 수호대 / 동일 조건 실력전":"고철 수호대 / 주간 성장 도전",20,23,404,30,14,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(plate,State.displayName,20,62,404,38,26,Ui.Ink,TextAnchor.MiddleCenter);
            CharacterArt.Draw(plate,107,113,230,234,skill?0:State.showcaseHero,State.selectedSkin,Outfits.Active(State,skill?0:State.showcaseHero));
            Ui.Label(plate,score.ToString("N0")+"점",20,358,404,69,42,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(plate,Social.Titles[State.showcaseTitle]+" · "+Social.Skins[State.selectedSkin],20,440,404,48,17,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(plate,week+" 주간 · "+(!Online?"로컬 체험":State.testProgress?"테스트 기록":"서버 저장 기록"),20,506,404,25,13,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(modalCard,"카드 이미지 공유",24,715,444,55,()=>{if(!exportingCard)StartCoroutine(ExportCard(plate));},null,score>0,18);
            Ui.Label(modalCard,score>0?"내 이름·도색·칭호와 주간 최고 점수가 포함됩니다.":"주간 도전 기록을 먼저 저장하세요.",24,785,444,36,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void SupportArrivalCard()
        {
            var support=combat.Support;bool show=support.showIdentity;
            if(show && support.outfit>0)OutfitVisuals.Special(effectLayer,new Vector2(combat.Position.x,-combat.Position.y),90,support.outfit);
            // HUD-only entrance: never cover the arena's attack warnings or consume touches.
            StylePresentation.Arrival(screen,18,16,show?support.name+" · 지원 도착":"길드 지원 도착",support.hero,show?support.skin:0,show?support.outfit:0,
                show?Social.Titles[Mathf.Clamp(support.title,0,Social.Titles.Length-1)]+" · "+SupportNames[support.hero]:"비공개 대원 · "+SupportNames[support.hero],combat);
        }
    }
}
