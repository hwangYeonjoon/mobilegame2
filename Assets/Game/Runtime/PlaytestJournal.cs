using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScrapGuard
{
    [Serializable] public sealed class PlaytestEvent
    {
        public string id, session, at, kind, subject, version, platform;
        public int number; public float value; public bool test;
    }
    [Serializable] public sealed class PlaytestData
    {
        public bool consent;
        public string participant=Guid.NewGuid().ToString("N");
        public List<PlaytestEvent> events=new List<PlaytestEvent>();
    }
    // Opt-in, local only. No auth token, nickname, public ID, device ID or arbitrary user text.
    public sealed class PlaytestJournal
    {
        public const int Limit=2000;
        readonly string path, session=Guid.NewGuid().ToString("N");
        readonly Func<DateTimeOffset> now;
        public PlaytestData Data {get;private set;}
        public PlaytestJournal(string path,Func<DateTimeOffset> clock=null)
        {
            this.path=path;now=clock??(()=>DateTimeOffset.UtcNow);Data=new PlaytestData();
            try {if(File.Exists(path))Data=JsonUtility.FromJson<PlaytestData>(File.ReadAllText(path))??new PlaytestData();}catch(IOException){}catch(ArgumentException){}
            if(Data.events==null)Data.events=new List<PlaytestEvent>();Prune();if(File.Exists(path))Save();
        }
        public void Consent(bool enabled){Data.consent=enabled;Save();}
        public void Clear(){bool consent=Data.consent;Data=new PlaytestData{consent=consent};Save();}
        static bool Safe(string s)
        {
            if(string.IsNullOrEmpty(s)||s.Length>48)return false;
            foreach(char c in s)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))return false;
            return true;
        }
        public void Add(string kind,string subject,int number=0,float value=0,bool test=false)
        {
            if(!Data.consent||!Safe(kind)||!Safe(subject)||float.IsNaN(value)||float.IsInfinity(value))return;
            Data.events.Add(new PlaytestEvent{id=Guid.NewGuid().ToString("N"),session=session,at=now().ToUniversalTime().ToString("O"),kind=kind,subject=subject,number=number,value=value,test=test,version=Application.version,platform=Application.platform.ToString()});Prune();Save();
        }
        void Prune()
        {
            var cutoff=now().AddDays(-14);Data.events.RemoveAll(e=>e==null||!DateTimeOffset.TryParse(e.at,out var at)||at<cutoff);
            if(Data.events.Count>Limit)Data.events.RemoveRange(0,Data.events.Count-Limit);
        }
        void Save()
        {
            try {Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",JsonUtility.ToJson(Data));if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);}
            catch(IOException){}catch(UnauthorizedAccessException){}
        }
        public string Export()
        {
            Prune();string output=Path.Combine(Path.GetDirectoryName(path),"playtest-export.json");
            File.WriteAllText(output,JsonUtility.ToJson(Data,true));return output;
        }
    }
}
