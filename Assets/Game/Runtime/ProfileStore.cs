using System;
using System.IO;
using ScrapGuard.Core;
using Social = ScrapGuard.Core.Social;
using UnityEngine;

namespace ScrapGuard
{
    public sealed class ProfileStore
    {
        readonly string path;
        public Economy Economy { get; private set; }
        public string Notice { get; private set; }
        public bool ReadOnly { get; private set; }
        public GameState State { get { return Economy.State; } }
        public ProfileStore(string path)
        {
            this.path = path;
            GameState loaded = TryLoad(path);
            if (loaded == null && File.Exists(path + ".bak"))
            {
                loaded = TryLoad(path + ".bak");
                if (loaded != null) Notice = "백업에서 진행 상황을 복구했어요.";
            }
            if (loaded == null && (File.Exists(path) || File.Exists(path + ".bak")))
            {
                Notice = "저장 파일을 읽을 수 없어 임시 체험으로 시작합니다. 기존 파일은 보존됩니다.";
                ReadOnly = true;
            }
            Economy = new Economy(loaded ?? GameState.New(), () => DateTimeOffset.UtcNow);
            Social.Normalize(State);
            if (!string.IsNullOrEmpty(State.activeRun)) { State.activeRun = ""; Notice = "종료되지 않은 전투가 있어 정비소로 돌아왔어요."; }
        }
        GameState TryLoad(string filename)
        {
            if (!File.Exists(filename)) return null;
            try
            {
                var s = JsonUtility.FromJson<GameState>(File.ReadAllText(filename));
                if (s == null || s.version != 1 || s.items == null || s.equipped == null || s.equipped.Count != 3 || s.heroes == null || s.heroes.Count != 3 || s.receipts == null)
                    return null;
                if (s.gold < 0 || s.parts < 0 || s.gems < 0 || s.selectedHero < 0 || s.selectedHero > 2 || !s.heroes[s.selectedHero].unlocked) return null;
                foreach (var item in s.items) if (item.definition < 0 || item.definition >= Catalog.Items.Length || item.rarity < 0 || item.rarity > 2 || item.level < 1 || item.level > 20) return null;
                for (int slot = 0; slot < 3; slot++) { var item = s.items.Find(i => i.id == s.equipped[slot]); if (item == null || Catalog.Items[item.definition].Slot != slot) return null; }
                return s;
            }
            catch (Exception e) { Debug.LogWarning("Profile read failed: " + e.GetType().Name); return null; }
        }
        public CommandResult Change(Func<Economy, CommandResult> command)
        {
            string before = JsonUtility.ToJson(State);
            try
            {
                var result = command(Economy);
                if (!result.Success) { Economy = new Economy(JsonUtility.FromJson<GameState>(before), () => DateTimeOffset.UtcNow); return result; }
                if (!ReadOnly) Save();
                return result;
            }
            catch (Exception e)
            {
                Economy = new Economy(JsonUtility.FromJson<GameState>(before), () => DateTimeOffset.UtcNow);
                Debug.LogError("Profile transaction failed: " + e.GetType().Name);
                return CommandResult.Fail("저장하지 못했어요. 저장 공간을 확인하고 다시 시도해 주세요.");
            }
        }
        public void Save()
        {
            if (ReadOnly) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(State, true));
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
        public void ReplaceRemote(GameState state)
        {
            if (state == null) throw new ArgumentException("Missing server state");
            Social.Normalize(state);
            Economy = new Economy(state, () => DateTimeOffset.UtcNow, false);
            // Remote data is a disposable cache; the server remains authoritative.
            try { Save(); } catch (IOException) { Debug.LogWarning("Remote cache write failed; reconnect to restore."); }
        }
    }
}
