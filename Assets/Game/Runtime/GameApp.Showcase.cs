using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using Social = ScrapGuard.Core.Social;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        RectTransform showcaseCard;
        bool exportingCard;
        string lastSharedPath;
        Color FrameColor(int frame) { return frame == 4 ? Ui.Hex("C89BFF") : frame == 1 || frame == 3 ? Ui.Gold : frame == 2 ? Ui.Blue : Ui.Mint; }
        void Showcase()
        {
            var body=Ui.Scroll(screen,24,176,492,MenuBottom-176,1120);
            var profile=Social.Build(State,Online?cloud.Session.playerId:"",!Online);
            showcaseCard=DrawProfile(body,profile,0,0);
            Ui.Button(body,RobotAssembly.Valid(profile.robotDesign)?"전시한 로봇 · 조립실에서 보기":"확대 감상 · 움직임과 공격 연출",0,548,492,72,()=>{if(RobotAssembly.Valid(profile.robotDesign))RobotHangar();else GuardianGallery(profile,()=>Show("showcase"));},Ui.Gold,true,22);
            Ui.Button(body,"내 프로필 꾸미기",0,636,238,72,CustomizeProfile,Ui.Raised,true,20);
            Ui.Button(body,"카드 이미지 공유",254,636,238,72,()=>{if(!exportingCard)StartCoroutine(ExportCard(showcaseCard));},null,true,20);
            Ui.Button(body,"장비 · 기록 보기",0,724,238,72,()=>ProfileEquipment(profile),Ui.Raised,true,20);
            Ui.Button(body,"다른 수호대 구경",254,724,238,72,()=>{if(Online&&cloudReady)StartCoroutine(LoadGallery(""));else AccountPanel();},Ui.Raised,true,20);
            Ui.Button(body,"전시 배경 · 플레이 기념관",0,812,492,72,()=>ThemeCollection(State.showcaseTheme),Ui.Raised,true,21);
            activityButton=Ui.Button(body,"응원 · 지원 활약 보기",0,900,492,72,OpenActivity,Ui.Mint,true,21);
            Ui.Button(body,"내가 조립한 로봇 · 9가지 조합",0,986,492,72,()=>RobotHangar(),Ui.Gold,true,21);
            if(Online&&cloudReady&&!smokeActive)StartCoroutine(RefreshActivity(false));
        }
        RectTransform DrawProfile(Transform parent,PublicProfile p,float x,float y)
        {
            Color accent=FrameColor(p.frame);
            var card=Ui.Box(parent,x,y,492,530,accent,true,"ShowcaseCard").rectTransform;
            Ui.Box(card,3,3,486,524,Ui.Background);
            Ui.Label(card,"SCRAP GUARD / MY GUARDIAN",24,13,444,24,14,accent,TextAnchor.MiddleCenter);
            Ui.Label(card,p.name,24,43,444,43,30,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"〈 "+Social.Titles[p.title]+" 〉",24,90,444,30,20,accent,TextAnchor.MiddleCenter);
            var stage=ShowcaseStage.Draw(card,18,129,456,272,p.theme,accent);
            if(p.frame==4)
            {
                var orbit=CombatRing.Create(stage,230,Ui.Hex("C89BFF"),.02f);orbit.anchoredPosition=new Vector2(228,-132);
            }
            if(RobotAssembly.Valid(p.robotDesign))RobotRigView.Draw(stage,100,3,255,p.robotDesign,p.robotRank,p.robotStyle);
            else {var art=CharacterArt.Draw(stage,80,p.theme==0?4:-6,296,p.theme==0?253:226,p.hero,p.skin,p.outfit);art.gameObject.AddComponent<ShowroomMotion>();}
            Ui.Label(card,RobotAssembly.Valid(p.robotDesign)?RobotAssembly.Name(p.robotDesign):StylePresentation.Look(p.outfit,p.skin),24,409,444,30,23,p.outfit>0?OutfitVisuals.Accent(p.outfit):accent,TextAnchor.MiddleCenter);
            Ui.Label(card,RobotAssembly.Valid(p.robotDesign)?"전투에서 직접 조립한 로봇":"Lv."+p.heroLevel+" "+Catalog.HeroNames[p.hero],24,445,444,28,20,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"작전 정복 "+p.stages+" / "+Campaign.Count,24,478,444,29,22,accent,TextAnchor.MiddleCenter);
            string status=p.local?"로컬 체험 기록":(p.test?"테스트 · 공개 ID ":"공개 ID ")+p.id;
            Ui.Label(card,status,24,508,444,18,12,Ui.Muted,TextAnchor.MiddleCenter);
            return card;
        }
        void ProfileEquipment(PublicProfile p)
        {
            var card=Dialog("전시 장비와 성장 기록",p.name+" · 전시 장비는 출격 장비와 다를 수 있어요.",770);
            Ui.Label(card,"공격 "+p.attack+"   체력 "+p.health,24,136,444,42,25,Ui.Mint,TextAnchor.MiddleCenter);
            for(int i=0;i<p.items.Count&&i<3;i++)
            {
                var item=p.items[i];float y=197+i*125;
                var row=Ui.Box(card,24,y,444,110,Ui.Background).transform;
                ItemIcon(row,new Item{definition=item.definition,rarity=item.rarity,level=item.level},12,13,84);
                Ui.Label(row,Catalog.Items[item.definition].Name,112,16,316,34,23);
                Ui.Label(row,Catalog.RarityNames[item.rarity]+" · Lv."+item.level+" · "+InventoryArt.Slot(Catalog.Items[item.definition].Slot),112,62,316,30,19,Rarity(item.rarity));
            }
            Ui.Label(card,"정복 "+p.stages+" / "+Campaign.Count+"  ·  처치 "+p.kills.ToString("N0")+"\n누적 출석 "+p.attendance+"일",24,589,444,69,21,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,"트로피 전시대",24,679,444,65,()=>TrophyRoom(p),Ui.Gold,true,21);
        }
        void CustomizeProfile()
        {
            Social.Normalize(State);
            int heroExtra=((HeroCatalog.Count+2)/3-1)*57,skinExtra=(Social.Skins.Length-8)*57,titleExtra=(Social.Titles.Length-6)*57;
            var card = Dialog("나를 보여 주는 전시실", "업적은 플레이로, 도색과 프레임은 취향대로.\n전용 의상은 상점 → 스타일 → 의상 체험에서 관리해요.", 870);
            var content = Ui.Scroll(card, 24, 137, 444, 699, 1987+heroExtra+skinExtra+titleExtra);
            Ui.Label(content, "닉네임", 0, 0, 444, 30, 17, Ui.Mint);
            var name = Ui.Input(content, State.displayName, "닉네임 2~12자", 0, 38, 300, 12);
            Ui.Button(content, "저장", 314, 38, 130, 52, () => Change(new GameCommand("rename") { value = name.text }, CustomizeProfile), Ui.Raised, true, 16);
            Ui.Label(content, "대표 수호대원", 0, 108, 444, 33, 17, Ui.Mint);
            for (int i = 0; i < HeroCatalog.Count; i++)
            {
                int hero = i;
                Ui.Button(content, Catalog.HeroNames[i] + (State.showcaseHero == i ? " ✓" : ""), (i%3)*150, 150+(i/3)*57, 144, 49,
                    () => Customize(hero: hero), State.showcaseHero == i ? Ui.Mint : Ui.Raised, State.heroes[i].unlocked, 13);
            }
            Ui.Label(content, "전시 장비는 출격 장비와 따로 보관돼요.\n판매한 전시 장비는 현재 장착 장비로 대체됩니다.", 0, 212+heroExtra, 444, 64, 14, Ui.Muted);
            Ui.Button(content, "현재 장착한 3종으로 전시 갱신", 0, 288+heroExtra, 444, 52, () => Change(new GameCommand("showcase_equipment"), CustomizeProfile), Ui.Raised, true, 17);
            Ui.Label(content, "플레이로 얻는 칭호", 0, 358+heroExtra, 444, 33, 17, Ui.Mint);
            for (int i = 0; i < Social.Titles.Length; i++)
            {
                int title = i; bool owned = Social.HasTitle(State, i);
                Ui.Button(content, Social.Titles[i] + (State.showcaseTitle == i ? " ✓" : ""), 0, 402+heroExtra + i * 57, 226, 47, () => Customize(title: title), State.showcaseTitle == i ? Ui.Mint : Ui.Raised, owned, 15);
                Ui.Label(content, Social.TitleGoals[i], 240, 402+heroExtra + i * 57, 204, 47, 13, owned ? Ui.Ink : Ui.Muted);
            }
            Ui.Label(content, "프로필 프레임", 0, 757+heroExtra+titleExtra, 444, 33, 17, Ui.Mint);
            string[] frameGoals = { "기본 제공", "첫 구역 클리어", "누적 출석 7일", "확정 구매 · ◆ 250", "코어 파편 20개 교환" };
            for (int i = 0; i < Social.Frames.Length; i++)
            {
                int frame = i; bool owned = Social.HasFrame(State, i);
                Ui.Button(content, Social.Frames[i] + (State.showcaseFrame == i ? " ✓" : ""), 0, 799+heroExtra+titleExtra + i * 57, 226, 47,
                    () => { if (owned) Customize(frame: frame); else if(frame==4)CoreExchange();else CosmeticOffer("frame_solar"); }, State.showcaseFrame == i ? Ui.Mint : Ui.Raised, owned || i >= 3, 15);
                Ui.Label(content, frameGoals[i], 240, 799+heroExtra+titleExtra + i * 57, 204, 47, 13, Ui.Muted);
            }
            Ui.Label(content, Outfits.Active(State,0)==1?"대원 도색 · 루크는 장착 의상이 우선":"대원 도색 · 정비소와 전투에도 적용", 0, 1097+heroExtra+titleExtra, 444, 33, 17, Ui.Mint);
            string[] skinGoals = { "기본 제공", "확정 구매 · ◆ 300", "적 1,000마리 처치" };
            for (int i = 0; i < Social.Skins.Length; i++)
            {
                int skin = i; bool owned = Social.HasSkin(State, i);
                Ui.Button(content, Social.Skins[i] + (State.selectedSkin == i ? " ✓" : ""), 0, 1139+heroExtra+titleExtra + i * 57, 226, 47,
                    () => { if (owned) Customize(skin: skin); else Wardrobe(skin); }, State.selectedSkin == i ? Ui.Mint : Ui.Raised, owned || i == 1 || i>=3, 15);
                Ui.Label(content, (i<3?skinGoals[i]:Campaign.Regions[i-3]+" 설계도 8개"), 240, 1139+heroExtra+titleExtra + i * 57, 204, 47, 13, Ui.Muted);
            }
            Ui.Label(content, "프로필 공개", 0, 1617+heroExtra+titleExtra+skinExtra, 444, 33, 17, Ui.Mint);
            Ui.Label(content, Online ? "공개하면 닉네임·대원·장비·성장 기록을\n이 서버의 다른 플레이어가 보고 응원할 수 있어요." : "로컬 카드는 이미지로 공유할 수 있어요.\n전시 광장 공개는 서버 계정에서 가능합니다.", 0, 1659+heroExtra+titleExtra+skinExtra, 444, 65, 15, Ui.Muted);
            Ui.Button(content, !Online ? "서버 계정 연결하기" : State.publicProfile ? "비공개로 바꾸기" : "프로필 공개하기", 0, 1743+heroExtra+titleExtra+skinExtra, 444, 54,
                () => { if (!Online) AccountPanel(); else if (State.publicProfile) Change(new GameCommand("visibility") { enabled = false }, CustomizeProfile); else ConfirmPublish(); }, Ui.Raised, true, 17);
            Ui.Label(content, "응원 수에 재화나 전투 보상은 연결되지 않습니다.\n획득 조건과 가격은 프로토타입 밸런스입니다.", 0, 1819+heroExtra+titleExtra+skinExtra, 444, 66, 14, Ui.Muted);
            Ui.Button(content, "전시실로 돌아가기", 0, 1909+heroExtra+titleExtra+skinExtra, 444, 54, () => Show("showcase"));
        }
        void Customize(int hero = -1, int title = -1, int frame = -1, int skin = -1)
        {
            Change(new GameCommand("customize") { hero = hero < 0 ? State.showcaseHero : hero, title = title < 0 ? State.showcaseTitle : title,
                frame = frame < 0 ? State.showcaseFrame : frame, skin = skin < 0 ? State.selectedSkin : skin }, CustomizeProfile);
        }
        void CosmeticOffer(string id)
        {
            bool frame = id == "frame_solar";
            var card = Dialog(frame ? "태양의 테두리" : "앰버 에디션", "확정 꾸미기 상품 · 능력치 변화 없음", 710);
            Ui.Box(card,28,136,436,335,frame ? Ui.Gold : Ui.Raised);
            Ui.Box(card,32,140,428,327,Ui.Background);
            CharacterArt.Draw(card,126,158,240,286,State.showcaseHero,frame ? State.selectedSkin : 1);
            Ui.Label(card,frame ? "전시 프로필 전용 프레임" : "전체 대원 공용 · 정비소·전투·전시에 적용",24,485,444,40,16,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Label(card,"확정 구성 1종 · 보석 " + (frame ? "250" : "300") + "개",24,539,444,44,22,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card, "보석으로 구매", 24, 616, 444, 60, () => Change(new GameCommand("cosmetic_buy") { value = id }, () => Wardrobe(frame ? State.selectedSkin : 1)));
        }
        void ConfirmPublish()
        {
            var card = Dialog("프로필을 공개할까요?", "공개 ID로 검색하거나 전시 광장에서 볼 수 있어요.", 430);
            Ui.Label(card, "공개: 닉네임, 대표 대원, 전시 장비,\n칭호·외형과 클리어·처치·출석 기록\n\n재화 잔액과 개인 복구 코드는 공개되지 않아요.", 24, 140, 444, 143, 16, Ui.Muted);
            Ui.Button(card, "이 프로필 공개하기", 24, 320, 444, 59, () => Change(new GameCommand("visibility") { enabled = true }, CustomizeProfile));
        }
        IEnumerator LoadGallery(string after)
        {
            if (networkBusy) yield break;
            SetNetworkBusy(true); ProfileList list = null; string error = "";
            yield return cloud.Get("/v1/profiles?after=" + Uri.EscapeDataString(after), (body,status) =>
            { if (status == 200) { try { list = JsonUtility.FromJson<ProfileList>(body); } catch { } } else error = cloud.Parse(body,status).message; });
            SetNetworkBusy(false);
            if (list == null) { Toast(string.IsNullOrEmpty(error) ? "전시 목록을 읽지 못했어요." : error); yield break; }
            Gallery(list);
        }
        void Gallery(ProfileList list)
        {
            var card = Dialog("수호대 전시 광장", "같은 서버에 공개한 플레이어의 성장 기록이에요.\n서로의 장비를 구경하고 응원을 보내세요.", 868);
            var search = Ui.Input(card, "", "공개 ID 12자리", 24, 138, 300, 12);
            Ui.Button(card, "찾기", 336, 138, 132, 52, () => StartCoroutine(LoadPublic(search.text.Trim().ToUpperInvariant())), Ui.Raised, true, 16);
            var featured=list.profiles.Find(p=>p.id==list.featuredId);
            if(featured!=null)
            {
                var pick=Ui.Box(card,24,205,444,128,Ui.Background).transform;
                CharacterArt.Draw(pick,6,11,95,105,featured.hero,featured.skin,featured.outfit);
                Ui.Label(pick,"이번 주 발견 · "+featured.name,113,8,312,30,20,Ui.Gold);
                Ui.Label(pick,"이 목록에서 주간 선정 · 구매 여부와 무관",113,43,312,28,12,Ui.Muted);
                Ui.Button(pick,"전시실 방문",113,79,312,38,()=>StartCoroutine(LoadPublic(featured.id)),Ui.Mint,true,16);
            }
            var content = Ui.Scroll(card, 24, featured==null?212:347, 444, featured==null?532:397, Math.Max(532, list.profiles.Count * 145));
            if (list.profiles.Count == 0) Ui.Label(content, "아직 볼 수 있는 프로필이 없어요.\n내 프로필을 공개하고 친구에게 ID를 알려 주세요.", 8, 165, 428, 110, 17, Ui.Muted, TextAnchor.MiddleCenter);
            for (int i = 0; i < list.profiles.Count; i++)
            {
                var p = list.profiles[i]; var row = Ui.Box(content, 0, i * 145, 444, 133, Ui.Raised).transform;
                CharacterArt.Draw(row,4,7,104,118,p.hero,p.skin,p.outfit);
                Ui.Label(row, p.name, 115, 8, 215, 30, 20);
                Ui.Label(row,StylePresentation.Look(p.outfit,p.skin),115,42,215,27,15,p.outfit>0?OutfitVisuals.Accent(p.outfit):Ui.Muted);
                Ui.Label(row, Social.Titles[p.title] + "\n정복 " + p.stages + "/" + Campaign.Count + " · 응원 " + p.likes, 115, 77, 215, 44, 12, FrameColor(p.frame));
                Ui.Button(row, "보기", 337, 40, 93, 52, () => StartCoroutine(LoadPublic(p.id)), null, true, 16);
            }
            Ui.Button(card, "처음부터", 24, 769, 138, 57, () => StartCoroutine(LoadGallery("")), Ui.Raised, true, 17);
            Ui.Button(card, "다음 목록", 177, 769, 138, 57, () => StartCoroutine(LoadGallery(list.next)), null, !string.IsNullOrEmpty(list.next), 17);
            Ui.Button(card,"차단 관리",330,769,138,57,() => StartCoroutine(LoadBlocks()),Ui.Raised,true,17);
        }
        IEnumerator LoadBlocks()
        {
            if (networkBusy) yield break;
            SetNetworkBusy(true); BlockList list = null;
            yield return cloud.Get("/v1/blocks", (body,status) => { if (status == 200) list = JsonUtility.FromJson<BlockList>(body); });
            SetNetworkBusy(false);
            if (list == null) { Toast("차단 목록을 불러오지 못했어요."); yield break; }
            var card = Dialog("차단 관리", "공개 ID로 표시됩니다. 해제하면 다시 볼 수 있어요.", 720);
            var content = Ui.Scroll(card,24,138,444,542,Math.Max(542,list.ids.Count * 76));
            if (list.ids.Count == 0) Ui.Label(content,"차단한 프로필이 없어요.",0,180,444,60,18,Ui.Muted,TextAnchor.MiddleCenter);
            for (int i = 0; i < list.ids.Count; i++)
            {
                string id = list.ids[i]; Ui.Label(content,id,0,i*76,270,55,19);
                Ui.Button(content,"차단 해제",282,i*76,162,55,() => StartCoroutine(SocialAction(id,"block",new GameCommand { enabled = false }, () => StartCoroutine(LoadBlocks()))),Ui.Raised,true,15);
            }
        }
        IEnumerator LoadPublic(string id)
        {
            if (networkBusy) yield break;
            if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^[A-F0-9]{12}$")) { Toast("공개 ID 12자리를 확인해 주세요."); yield break; }
            SetNetworkBusy(true); PublicProfile profile = null; string error = "";
            yield return cloud.Get("/v1/profiles/" + id, (body,status) =>
            { if (status == 200) { try { profile = JsonUtility.FromJson<PublicProfile>(body); } catch { } } else error = cloud.Parse(body,status).message; });
            SetNetworkBusy(false);
            if (profile == null) Toast(string.IsNullOrEmpty(error) ? "프로필을 읽지 못했어요." : error); else PublicPanel(profile);
        }
        void PublicPanel(PublicProfile p)
        {
            Track("profile_view","public");
            var card=Dialog("수호대 기록","공개 ID "+p.id+" · 응원 "+p.likes,900);
            var body=Ui.Scroll(card,24,130,444,744,815);
            var preview=DrawProfile(body,p,0,0);preview.localScale=Vector3.one*(444f/492f);
            Ui.Button(body,"확대 감상 · 움직임과 공격 연출",0,493,444,70,()=>GuardianGallery(p,()=>PublicPanel(p)),Ui.Gold,true,21);
            bool other=p.id!=cloud.Session.playerId;
            Ui.Button(body,p.liked?"응원 취소":"멋져요! 응원하기",0,575,444,70,
                ()=>StartCoroutine(SocialAction(p.id,"like",new GameCommand{enabled=!p.liked},()=>StartCoroutine(LoadPublic(p.id)))),null,other,21);
            Ui.Button(body,"전시 장비 · 기록 · 트로피",0,657,444,64,()=>ProfileEquipment(p),Ui.Raised,true,20);
            Ui.Button(body,"신고",0,733,140,70,()=>ReportPanel(p.id),Ui.Raised,other,20);
            Ui.Button(body,"차단",152,733,140,70,()=>BlockPanel(p.id),Ui.Raised,other,20);
            Ui.Button(body,"광장으로",304,733,140,70,()=>StartCoroutine(LoadGallery("")),Ui.Raised,true,19);
        }
        IEnumerator SocialAction(string id, string action, GameCommand command, Action after)
        {
            if (networkBusy) yield break;
            SetNetworkBusy(true); bool ok = false; string error = "";
            yield return cloud.Send("/v1/profiles/" + id + "/" + action, "POST", command, (body,status) => { ok = status == 200; if (!ok) error = cloud.Parse(body,status).message; });
            SetNetworkBusy(false);
            if (ok) { Toast(action == "report" ? "신고를 접수했어요." : action == "block" ? "차단 설정을 저장했어요." : "응원 설정을 저장했어요."); after?.Invoke(); } else Toast(error);
        }
        void ReportPanel(string id)
        {
            var card = Dialog("프로필 신고", "해당하는 사유를 선택해 주세요.\n신고는 서버에 접수됩니다.", 441);
            string[] reasons = { "name", "cheating", "other" }, labels = { "부적절한 닉네임", "부정한 플레이 의심", "기타 부적절한 프로필" };
            for (int i = 0; i < 3; i++) { string reason = reasons[i]; Ui.Button(card, labels[i], 24, 153 + i * 82, 444, 59, () => StartCoroutine(SocialAction(id, "report", new GameCommand { value = reason }, () => StartCoroutine(LoadPublic(id)))), Ui.Raised, true, 17); }
        }
        void BlockPanel(string id)
        {
            var card = Dialog("이 프로필을 차단할까요?", "서로의 프로필이 전시 목록과 검색에서 숨겨지고\n서로 보낸 응원이 취소됩니다.", 364);
            Ui.Button(card, "차단하기", 24, 165, 444, 57, () => StartCoroutine(SocialAction(id, "block", new GameCommand { enabled = true }, () => StartCoroutine(LoadGallery("")))), Ui.Raised);
            Ui.Button(card, "돌아가기", 24, 250, 444, 57, () => StartCoroutine(LoadPublic(id)));
        }
        IEnumerator ExportCard(RectTransform card)
        {
            if (exportingCard || card == null) yield break;
            exportingCard = true; toastRoot.gameObject.SetActive(false);
            // Export a full-size copy outside scrolling masks, even when the share action is below the fold.
            var exportRoot=Ui.Rect(Root,"Share capture",0,0,540,960);
            Ui.Box(exportRoot,0,-menuExtra/2,540,MenuHeight,Ui.Background,false).raycastTarget=true;
            var copy=Instantiate(card,exportRoot,false);copy.anchoredPosition=new Vector2(24,-215);card=copy;
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = null, crop = null;
            try
            {
                var corners = new Vector3[4]; card.GetWorldCorners(corners);
                int x = Mathf.Max(0, Mathf.RoundToInt(corners[0].x)), y = Mathf.Max(0, Mathf.RoundToInt(corners[0].y));
                int w = Mathf.Min(Screen.width - x, Mathf.RoundToInt(corners[2].x - corners[0].x));
                int h = Mathf.Min(Screen.height - y, Mathf.RoundToInt(corners[2].y - corners[0].y));
                screenshot = ScreenCapture.CaptureScreenshotAsTexture(); crop = new Texture2D(w, h, TextureFormat.RGB24, false);
                crop.SetPixels(screenshot.GetPixels(x, y, w, h)); crop.Apply();
                string folder = Path.Combine(Application.temporaryCachePath, "showcase"); Directory.CreateDirectory(folder);
                lastSharedPath = Path.Combine(folder, "scrap-guard-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".png");
                File.WriteAllBytes(lastSharedPath, crop.EncodeToPNG());
                foreach (string old in Directory.GetFiles(folder, "scrap-guard-*.png")) if (File.GetLastWriteTimeUtc(old) < DateTime.UtcNow.AddDays(-7)) File.Delete(old);
                if (!smokeActive) NativeShare.Image(lastSharedPath);
                Toast(Application.platform == RuntimePlatform.Android ? "공유할 앱을 선택해 주세요." : "카드 PNG 저장 완료 · 계정 화면의 공개 ID로 검색 가능");
                Debug.Log("SHOWCASE_EXPORTED " + lastSharedPath);
            }
            catch (Exception ex) { Toast("카드 저장 또는 공유를 시작하지 못했어요."); Debug.LogWarning("Share: " + ex.GetType().Name); }
            finally { if (screenshot != null) Destroy(screenshot); if (crop != null) Destroy(crop); exportingCard = false; Destroy(exportRoot.gameObject); }
        }
    }
}
