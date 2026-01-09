using System;
using UnityEngine;

[Serializable]
public sealed class SongDefinition
{
    public string songId = "dance-mood";

    public string songName = "Dance Mood";

    public AudioClip musicClip;

    public MusicSource musicSource = MusicSource.Original;

    public string chartFileName = "chart.json";
}
