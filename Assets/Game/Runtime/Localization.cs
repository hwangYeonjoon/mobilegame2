using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ScrapGuard
{
    // Presentation only. Never translate identifiers, profiles, replay inputs or core rules.
    // Korean source phrases remain keys so older server messages can use the same catalog.
    public static class Localization
    {
        public const string PreferenceKey = "scrapguard.language.v1";
        public static event Action Changed;
        public static string Preference { get; private set; } = "system";
        public static bool English { get; private set; }
        [Serializable] public sealed class Entry { public string ko, en; }
        [Serializable] sealed class Catalog { public Entry[] entries; }
        sealed class Node
        {
            public readonly Dictionary<char, Node> next = new Dictionary<char, Node>();
            public string value;
        }
        static Node root;
        static readonly Dictionary<string, string> exact = new Dictionary<string, string>(StringComparer.Ordinal);
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.Ordinal);
        public static int EntryCount { get { Load(); return exact.Count; } }
        public static bool HasTranslation(string source) { Load(); return exact.ContainsKey(source); }
        public static bool IsEnglish(string preference, SystemLanguage system)
        { return preference == "en" || preference != "ko" && system != SystemLanguage.Korean; }
        public static void Initialize()
        {
            Load();
            SetLanguage(PlayerPrefs.GetString(PreferenceKey, "system"), false);
        }
        public static void SetLanguage(string preference, bool persist = true)
        {
            if (preference != "system" && preference != "ko" && preference != "en") preference = "system";
            Preference = preference; English = IsEnglish(preference, Application.systemLanguage);
            if (persist) { PlayerPrefs.SetString(PreferenceKey, preference); PlayerPrefs.Save(); }
            Changed?.Invoke();
        }
        static void Load()
        {
            if (root != null) return;
            root = new Node();
            var asset = Resources.Load<TextAsset>("Localization/en");
            if (asset == null) throw new InvalidOperationException("Missing English localization catalog");
            var catalog = JsonUtility.FromJson<Catalog>(asset.text);
            foreach (var entry in catalog.entries)
            {
                if (string.IsNullOrEmpty(entry.ko) || entry.en == null || exact.ContainsKey(entry.ko))
                    throw new InvalidOperationException("Invalid or duplicate localization key");
                exact.Add(entry.ko, entry.en);
                var node = root;
                foreach (char c in entry.ko)
                {
                    if (!node.next.TryGetValue(c, out var child)) node.next[c] = child = new Node();
                    node = child;
                }
                node.value = entry.en;
            }
        }
        // Protect user-authored names, codes and external store prices from translation.
        // Markers exist only in a transient UI string and never enter saved/network data.
        public static string Verbatim(string value)
        { return "\uE000" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? "")) + "\uE001"; }
        public static bool ContainsKorean(string text)
        { if (text != null) foreach (char c in text) if (c >= '\uAC00' && c <= '\uD7A3' || c >= '\u3130' && c <= '\u318F') return true; return false; }
        public static string Text(string source)
        {
            if (string.IsNullOrEmpty(source)) return source ?? "";
            if (source.IndexOf('\uE000') < 0) return English ? Translate(source) : source;
            var output = new StringBuilder(); int start = 0;
            while (start < source.Length)
            {
                int open = source.IndexOf('\uE000', start);
                if (open < 0) { output.Append(English ? Translate(source.Substring(start)) : source.Substring(start)); break; }
                string prefix = source.Substring(start, open - start);
                output.Append(English ? Translate(prefix) : prefix);
                int close = source.IndexOf('\uE001', open + 1);
                if (close < 0) { output.Append(source.Substring(open)); break; }
                try { output.Append(Encoding.UTF8.GetString(Convert.FromBase64String(source.Substring(open + 1, close - open - 1)))); }
                catch (FormatException) { output.Append(source.Substring(open, close - open + 1)); }
                start = close + 1;
            }
            return output.ToString();
        }
        static string Translate(string source)
        {
            if (!ContainsKorean(source)) return source;
            Load();
            if (exact.TryGetValue(source, out var translated)) return translated;
            if (cache.TryGetValue(source, out translated)) return translated;
            // Resolve overlapping concatenated phrases as a whole. A leading fragment such
            // as " 회수" must not consume the start of the longer "회수 반경 +" label.
            // Prefer complete Hangul coverage, then longer phrases. Exact strings and the
            // bounded cache handle the usual path; this runs once per new composed label.
            int length=source.Length;
            var scores=new long[length+1];var ends=new int[length];var matches=new string[length];
            for(int i=length-1;i>=0;i--)
            {
                scores[i]=scores[i+1];ends[i]=i+1;
                var node=root;int korean=0;
                for (int j = i; j < source.Length && node.next.TryGetValue(source[j], out node); j++)
                {
                    char c=source[j];if(c>='\uAC00'&&c<='\uD7A3'||c>='\u3130'&&c<='\u318F')korean++;
                    if(node.value==null)continue;
                    long score=scores[j+1]+korean*1000000L+(j-i+1L)*(j-i+1);
                    if(score>scores[i]){scores[i]=score;ends[i]=j+1;matches[i]=node.value;}
                }
            }
            var output = new StringBuilder(source.Length * 2);
            for(int i=0;i<length;i=ends[i])output.Append(matches[i]??source[i].ToString());
            translated = output.ToString();
            // A future server may send a new phrase. Preserve that message intact instead
            // of showing a misleading mixture of translated syllables and Korean words.
            if(ContainsKorean(translated))translated=source;
            if (cache.Count >= 4096) cache.Clear();
            cache[source] = translated;
            return translated;
        }
    }
}
