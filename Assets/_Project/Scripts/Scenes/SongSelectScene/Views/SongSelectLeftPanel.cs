using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SongSelectLeftPanel : MonoBehaviour
{
    [Header("Song Header")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text artistText;

    [Header("Difficulty")]
    [SerializeField] RectTransform difficultyRoot;
    [SerializeField] TMP_Text difficultyHeaderText;

    [Header("Stats")]
    [SerializeField] TMP_Text bpmText;
    [SerializeField] TMP_Text lengthText;
    [SerializeField] TMP_Text notesText;

    readonly List<TMP_Text> difficultyEntries = new();

    static readonly ChartDifficulty[] DifficultyOrder =
    {
        ChartDifficulty.Beginner,
        ChartDifficulty.Easy,
        ChartDifficulty.Medium,
        ChartDifficulty.Hard,
        ChartDifficulty.Challenge,
    };

    static readonly Dictionary<ChartDifficulty, Color> DifficultyColors = new()
    {
        { ChartDifficulty.Beginner, new Color(0.78f, 0.4f, 1f) },
        { ChartDifficulty.Easy, new Color(0.3f, 0.9f, 0.4f) },
        { ChartDifficulty.Medium, new Color(1f, 0.9f, 0.35f) },
        { ChartDifficulty.Hard, new Color(1f, 0.4f, 0.3f) },
        { ChartDifficulty.Challenge, new Color(1f, 0.5f, 0.9f) },
    };

    void Awake()
    {
        EnsureRuntimeUi();
    }

    public void SetSong(SongMeta song, ChartDifficulty selectedDifficulty, Dictionary<ChartDifficulty, int> meters, Chart chart)
    {
        if (song == null) return;

        EnsureRuntimeUi();

        titleText.text = song.DisplayTitle;
        artistText.text = string.IsNullOrWhiteSpace(song.Artist) ? "Unknown Artist" : song.Artist;

        UpdateDifficultyList(selectedDifficulty, meters);
        UpdateStats(song, chart);
    }

    void EnsureRuntimeUi()
    {
        if (titleText != null && artistText != null && difficultyRoot != null && bpmText != null && lengthText != null && notesText != null)
            return;

        var root = new GameObject("LeftPanelContent", typeof(RectTransform));
        root.transform.SetParent(transform, false);

        var rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = new Vector2(0, 0);
        rootRect.anchorMax = new Vector2(1, 1);
        rootRect.offsetMin = new Vector2(32, 32);
        rootRect.offsetMax = new Vector2(-32, -32);

        var rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.childAlignment = TextAnchor.UpperLeft;
        rootLayout.spacing = 16f;
        rootLayout.padding = new RectOffset(0, 0, 0, 0);

        var header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(root.transform, false);
        var headerLayout = header.AddComponent<VerticalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.UpperLeft;
        headerLayout.spacing = 6f;

        titleText = CreateText("Title", header.transform, 36f, FontStyles.Bold);
        artistText = CreateText("Artist", header.transform, 20f, FontStyles.Normal);

        var difficultySection = new GameObject("Difficulty", typeof(RectTransform));
        difficultySection.transform.SetParent(root.transform, false);
        var difficultyLayout = difficultySection.AddComponent<VerticalLayoutGroup>();
        difficultyLayout.childAlignment = TextAnchor.UpperLeft;
        difficultyLayout.spacing = 4f;

        difficultyHeaderText = CreateText("DifficultyHeader", difficultySection.transform, 22f, FontStyles.Bold);
        difficultyHeaderText.text = "Difficulty";

        difficultyRoot = new GameObject("DifficultyList", typeof(RectTransform)).GetComponent<RectTransform>();
        difficultyRoot.SetParent(difficultySection.transform, false);
        var diffListLayout = difficultyRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        diffListLayout.childAlignment = TextAnchor.UpperLeft;
        diffListLayout.spacing = 2f;

        var statsSection = new GameObject("Stats", typeof(RectTransform));
        statsSection.transform.SetParent(root.transform, false);
        var statsLayout = statsSection.AddComponent<VerticalLayoutGroup>();
        statsLayout.childAlignment = TextAnchor.UpperLeft;
        statsLayout.spacing = 4f;

        bpmText = CreateText("Bpm", statsSection.transform, 20f, FontStyles.Normal);
        lengthText = CreateText("Length", statsSection.transform, 20f, FontStyles.Normal);
        notesText = CreateText("Notes", statsSection.transform, 20f, FontStyles.Normal);
    }

    TMP_Text CreateText(string name, Transform parent, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return text;
    }

    void UpdateDifficultyList(ChartDifficulty selectedDifficulty, Dictionary<ChartDifficulty, int> meters)
    {
        if (difficultyRoot == null) return;

        if (difficultyEntries.Count == 0)
        {
            foreach (var difficulty in DifficultyOrder)
            {
                var entry = CreateText($"Diff{difficulty}", difficultyRoot, 18f, FontStyles.Bold);
                entry.textWrappingMode = TextWrappingModes.NoWrap;
                difficultyEntries.Add(entry);
            }
        }

        for (int i = 0; i < difficultyEntries.Count; i++)
        {
            var difficulty = DifficultyOrder[i];
            var entry = difficultyEntries[i];
            var meterText = meters != null && meters.TryGetValue(difficulty, out var meter) ? meter.ToString() : "--";
            entry.text = $"{difficulty.ToString().ToUpperInvariant()}  {meterText}";

            var color = DifficultyColors.TryGetValue(difficulty, out var diffColor) ? diffColor : Color.white;
            entry.color = difficulty == selectedDifficulty ? color : color * 0.55f;
        }
    }

    void UpdateStats(SongMeta song, Chart chart)
    {
        if (bpmText == null || lengthText == null || notesText == null) return;

        var bpmDisplay = !string.IsNullOrWhiteSpace(song.DisplayBpm) ? song.DisplayBpm : "---";
        var lengthDisplay = "--:--";
        var notesDisplay = "--";

        if (chart != null)
        {
            if (chart.BpmChanges.Count > 0)
            {
                var min = chart.BpmChanges.Min(change => change.Bpm);
                var max = chart.BpmChanges.Max(change => change.Bpm);
                bpmDisplay = Math.Abs(max - min) < 0.01
                    ? $"{Mathf.RoundToInt((float)min)}"
                    : $"{Mathf.RoundToInt((float)min)}-{Mathf.RoundToInt((float)max)}";
            }

            if (chart.Notes.Count > 0)
            {
                var lastBeat = chart.Notes.Max(note => note.Beat);
                var seconds = chart.BeatToSeconds(lastBeat);
                var timeSpan = TimeSpan.FromSeconds(Math.Max(0, seconds));
                lengthDisplay = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                notesDisplay = chart.Notes.Count.ToString();
            }
            else
            {
                notesDisplay = "0";
            }
        }

        bpmText.text = $"BPM  {bpmDisplay}";
        lengthText.text = $"Length  {lengthDisplay}";
        notesText.text = $"Notes  {notesDisplay}";
    }
}
