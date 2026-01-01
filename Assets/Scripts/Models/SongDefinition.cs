using System;
using UnityEngine;

[Serializable]
public sealed class SongDefinition
{
    public string songId = "dance-mood";

    public AudioClip musicClip;

    public string chartFileName = "chart.json";
}
