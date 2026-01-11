using System;
using System.IO;
using UnityEngine;

// Metadata extracted from a StepMania .sm header.
[Serializable]
public sealed class SongMeta
{
    public string Title;
    public string Artist;
    public string Credit;
    public string MusicFile;
    public string BannerFile;
    public string BackgroundFile;
    public string SmFilePath;
    public string SongDirPath;
    public float? SampleStart;
    public float? SampleLength;
    public float? OffsetSeconds;
    public string DisplayBpm;
    public string Bpms;
    public AudioType MusicAudioType = AudioType.OGGVORBIS;
    public ChartDifficulty ChartDifficulty = ChartDifficulty.Beginner;

    [NonSerialized] public AudioClip MusicClip;
    [NonSerialized] public Texture2D BannerTexture;
    [NonSerialized] public Texture2D BackgroundTexture;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title)
        ? Path.GetFileNameWithoutExtension(SmFilePath)
        : Title;
}
