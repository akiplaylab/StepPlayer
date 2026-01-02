using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DDR/Song Library", fileName = "SongLibrary")]
public sealed class SongLibrary : ScriptableObject
{
    [SerializeField] List<SongDefinition> songs = new();

    public IReadOnlyList<SongDefinition> Songs => songs;

    public SongDefinition Get(int index)
    {
        if (songs == null || songs.Count == 0) return null;
        index = Mathf.Clamp(index, 0, songs.Count - 1);
        return songs[index];
    }

    public int Count => songs?.Count ?? 0;
}
