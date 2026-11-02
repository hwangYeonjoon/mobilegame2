using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScrapGuard.Core
{
    [Serializable] public class WeeklyProgress
    {
        public string week;
        public int bestScore, attempts, claimed;
    }
    [Serializable] public class RankEntry
    {
        public int rank, score, hero, skin;
        public string id, name;
    }
    [Serializable] public class WeeklyBoard
    {
        public string week;
        public int myRank, myScore;
        public List<RankEntry> entries = new List<RankEntry>();
    }
    [Serializable] public class GuildMember
    {
        public string id, name;
        public int hero = -1, level, skin, power;
        public bool leader, self;
    }
    [Serializable] public class GuildView
    {
        public string id, name, code;
        public int remaining;
        public bool leader;
        public List<GuildMember> members = new List<GuildMember>();
    }
    [Serializable] public class SupportSnapshot
    {
        public string owner, name;
        public int hero, skin, power;
    }
    public static class Endgame
    {
        public const string WeeklyMode = "weekly", PracticeMode = "practice";
        public const int Duration = 90;
        public static readonly int[] Shards = { 1, 3, 5, 8 };
        public static readonly int[] Goals = { 0, 5000, 15000, 30000 };
        public static readonly int[] Gold = { 1000, 1000, 1500, 2000 }, Parts = { 10, 20, 30, 40 }, Gems = { 0, 0, 0, 50 };
        public static readonly string[] TrophyNames = { "도전의 시작", "코어 탐색자", "코어 사냥꾼", "코어 정복자" };
        public static string Week(DateTimeOffset utc)
        {
            var date=DateTime.ParseExact(Catalog.ServiceDate(utc),"yyyy-MM-dd",CultureInfo.InvariantCulture);
            int offset=((int)date.DayOfWeek+6)%7;
            return date.AddDays(-offset).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        }
        public static int Seed(string week)
        {
            var date=DateTime.ParseExact(week,"yyyy-MM-dd",CultureInfo.InvariantCulture);
            return 40000+(int)(date-new DateTime(2026,1,1)).TotalDays;
        }
        public static bool Unlocked(GameState state) { return state.clearedStages.Contains(4); }
        public static bool IsChallenge(string mode) { return mode==WeeklyMode || mode==PracticeMode; }
        public static void Normalize(GameState state)
        {
            if(state.weekly==null)state.weekly=new List<WeeklyProgress>();
            if(state.activeMode==null)state.activeMode="";
        }
        public static WeeklyProgress Find(GameState state,string week)
        {
            Normalize(state);return state.weekly.Find(w=>w.week==week);
        }
        public static int Trophy(GameState state)
        {
            Normalize(state);int best=-1;
            foreach(var week in state.weekly) if(week.attempts>0)
                for(int tier=0;tier<Goals.Length;tier++) if(week.bestScore>=Goals[tier])best=Math.Max(best,tier);
            return best;
        }
    }
}
