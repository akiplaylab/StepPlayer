using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ChartLoader
{
    const int MaxSupportedBpm = 1000;

    public static Chart LoadFromStreamingAssets(string fileName, ChartDifficulty difficulty)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension is ".sm")
            return LoadFromSm(path, difficulty);

        throw new InvalidDataException($"Unsupported chart format: {extension}");
    }

    static Chart LoadFromSm(string path, ChartDifficulty difficulty)
    {
        var tags = SmFileCache.GetAllTags(path);

        var music = GetFirstTag(tags, "MUSIC");
        var offset = ParseDouble(GetFirstTag(tags, "OFFSET"), 0.0);
        var bpmChanges = ParseBpms(GetFirstTag(tags, "BPMS"));
        var noteData = GetNotesData(tags, difficulty);

        if (bpmChanges.Count == 0)
            bpmChanges.Add(new BpmChange(0, 120));

        bpmChanges.Sort((a, b) => a.Beat.CompareTo(b.Beat));
        if (bpmChanges[0].Beat > 0)
            bpmChanges.Insert(0, new BpmChange(0, bpmChanges[0].Bpm));

        var baseBpm = (int)Math.Round(bpmChanges[0].Bpm);
        if (baseBpm <= 0 || baseBpm > MaxSupportedBpm)
            throw new InvalidDataException($"Invalid bpm: {baseBpm} (must be between 1 and {MaxSupportedBpm})");

        var notes = ParseNotes(noteData);
        var ordered = notes.OrderBy(n => n.Beat).ToList();
        return new Chart(music, baseBpm, (float)(-offset), ordered, bpmChanges);
    }

    static string GetFirstTag(Dictionary<string, List<string>> tags, string name)
    {
        return tags.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : string.Empty;
    }

    static string GetNotesData(Dictionary<string, List<string>> tags, ChartDifficulty difficulty)
    {
        if (!tags.TryGetValue("NOTES", out var list))
            throw new InvalidDataException("No NOTES section found in simfile.");

        var difficultyName = ChartDifficultyMapper.ToSmName(difficulty);

        foreach (var entry in list)
        {
            var parts = entry.Split(new[] { ':' }, 6);
            if (parts.Length < 6) continue;

            var stepType = parts[0].Trim();
            var entryDifficulty = parts[2].Trim();

            if (stepType.Equals("dance-single", StringComparison.OrdinalIgnoreCase)
                && entryDifficulty.Equals(difficultyName, StringComparison.OrdinalIgnoreCase))
                return parts[5];
        }

        var fallback = (list.FirstOrDefault(entry =>
        {
            var parts = entry.Split(new[] { ':' }, 6);
            return parts.Length >= 6
                && parts[0].Trim().Equals("dance-single", StringComparison.OrdinalIgnoreCase);
        }) ?? list.FirstOrDefault()) ?? throw new InvalidDataException("No NOTES section found in simfile.");
        var fallbackParts = fallback.Split(new[] { ':' }, 6);
        return fallbackParts.Length >= 6 ? fallbackParts[5] : fallback;
    }

    static List<BpmChange> ParseBpms(string value)
    {
        var bpmChanges = new List<BpmChange>();
        if (string.IsNullOrWhiteSpace(value))
            return bpmChanges;

        var entries = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            var parts = entry.Split('=');
            if (parts.Length != 2) continue;
            var beat = ParseDouble(parts[0], 0);
            var bpm = ParseDouble(parts[1], 0);
            if (bpm <= 0) continue;
            bpmChanges.Add(new BpmChange(beat, bpm));
        }

        return bpmChanges;
    }

    static List<Note> ParseNotes(string noteData)
    {
        var notes = new List<Note>(1024);
        if (string.IsNullOrWhiteSpace(noteData))
            return notes;

        var normalized = noteData.Replace("\r", string.Empty);
        var measures = normalized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        for (int m = 0; m < measures.Length; m++)
        {
            var lines = measures[m]
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
                .ToList();

            if (lines.Count == 0) continue;

            for (int r = 0; r < lines.Count; r++)
            {
                var row = lines[r];
                for (int laneIndex = 0; laneIndex < 4 && laneIndex < row.Length; laneIndex++)
                {
                    if (!IsTapNote(row[laneIndex])) continue;

                    var beat = (m * 4.0) + ((double)r / lines.Count) * 4.0;
                    notes.Add(new Note(beat, (Lane)laneIndex));
                }
            }
        }

        return notes;
    }

    static bool IsTapNote(char value)
    {
        return value is '1' or '2' or '4';
    }

    static double ParseDouble(string value, double fallback)
    {
        return double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : fallback;
    }

}
