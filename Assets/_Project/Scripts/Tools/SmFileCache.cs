using System;
using System.Collections.Generic;
using System.IO;

public static class SmFileCache
{
    class CacheEntry
    {
        public string Content;
        public DateTime LastWriteTimeUtc;
        public Dictionary<string, string> HeaderTags;
        public Dictionary<string, List<string>> AllTags;
    }

    static readonly Dictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);
    static readonly object CacheLock = new();

    public static Dictionary<string, string> GetHeaderTags(string path)
    {
        var entry = GetOrLoadEntry(path);
        lock (CacheLock)
        {
            if (entry.HeaderTags == null)
            {
                entry.HeaderTags = SmTagParser.ParseHeader(entry.Content);
            }

            return entry.HeaderTags;
        }
    }

    public static Dictionary<string, List<string>> GetAllTags(string path)
    {
        var entry = GetOrLoadEntry(path);
        lock (CacheLock)
        {
            if (entry.AllTags == null)
            {
                entry.AllTags = SmTagParser.ParseAllTags(entry.Content);
            }

            return entry.AllTags;
        }
    }

    public static string GetContent(string path)
    {
        return GetOrLoadEntry(path).Content;
    }

    static CacheEntry GetOrLoadEntry(string path)
    {
        var fullPath = Path.GetFullPath(path);
        lock (CacheLock)
        {
            var lastWriteTimeUtc = File.GetLastWriteTimeUtc(fullPath);
            if (Cache.TryGetValue(fullPath, out var existing) && existing.LastWriteTimeUtc == lastWriteTimeUtc)
                return existing;

            var content = File.ReadAllText(fullPath);
            var entry = new CacheEntry
            {
                Content = content,
                LastWriteTimeUtc = lastWriteTimeUtc,
            };
            Cache[fullPath] = entry;
            return entry;
        }
    }
}
