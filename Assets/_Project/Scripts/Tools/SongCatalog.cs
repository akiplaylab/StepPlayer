using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// Scans StreamingAssets/Songs for .sm files and builds SongMeta entries.
public static class SongCatalog
{
    public static List<SongMeta> BuildCatalog()
    {
        var results = new List<SongMeta>();
        var root = Path.Combine(Application.streamingAssetsPath, "Songs");

        if (!Directory.Exists(root))
        {
            Debug.LogError($"Songs directory missing: {root}");
            return results;
        }

        var smFiles = Directory.GetFiles(root, "*.sm", SearchOption.AllDirectories);
        foreach (var smPath in smFiles)
        {
            var meta = TryBuildMeta(smPath);
            if (meta != null)
                results.Add(meta);
        }

        return results;
    }

    static SongMeta TryBuildMeta(string smPath)
    {
        string content;
        try
        {
            content = File.ReadAllText(smPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read .sm file: {smPath} ({ex.Message})");
            return null;
        }

        var tags = SmParser.ParseHeader(content);
        var songDir = Path.GetDirectoryName(smPath) ?? string.Empty;

        if (!tags.TryGetValue("MUSIC", out var musicTag) || string.IsNullOrWhiteSpace(musicTag))
        {
            Debug.LogError($"Missing tag #MUSIC in {smPath}.");
            return null;
        }

        if (!TryResolveMusicFile(smPath, songDir, musicTag, out var musicFile, out var audioType))
            return null;

        return new SongMeta
        {
            Title = GetTag(tags, "TITLE"),
            Artist = GetTag(tags, "ARTIST"),
            Credit = GetTag(tags, "CREDIT"),
            MusicFile = musicFile,
            BannerFile = GetTag(tags, "BANNER"),
            BackgroundFile = GetTag(tags, "BACKGROUND"),
            SmFilePath = smPath,
            SongDirPath = songDir,
            SampleStart = ParseOptionalFloat(GetTag(tags, "SAMPLESTART")),
            SampleLength = ParseOptionalFloat(GetTag(tags, "SAMPLELENGTH")),
            OffsetSeconds = ParseOptionalFloat(GetTag(tags, "OFFSET")),
            DisplayBpm = GetTag(tags, "DISPLAYBPM"),
            Bpms = GetTag(tags, "BPMS"),
            MusicAudioType = audioType,
        };
    }

    static string GetTag(Dictionary<string, string> tags, string key)
    {
        return tags.TryGetValue(key, out var value) ? value : null;
    }

    static float? ParseOptionalFloat(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }

    static bool TryResolveMusicFile(string smPath, string songDir, string musicTag, out string musicFile, out AudioType audioType)
    {
        musicFile = musicTag;
        var primaryPath = Path.Combine(songDir, musicFile);
        if (File.Exists(primaryPath))
        {
            audioType = AudioType.OGGVORBIS;
            return true;
        }

        var fallbackFile = Path.ChangeExtension(musicFile, ".mp3");
        if (!string.Equals(fallbackFile, musicFile, StringComparison.OrdinalIgnoreCase))
        {
            var fallbackPath = Path.Combine(songDir, fallbackFile);
            if (File.Exists(fallbackPath))
            {
                musicFile = fallbackFile;
                audioType = AudioType.MPEG;
                return true;
            }
        }

        Debug.LogError($"Music file missing: {smPath} tag #MUSIC file '{musicTag}' (fallback '{fallbackFile}' not found).");
        audioType = AudioType.OGGVORBIS;
        return false;
    }
}
