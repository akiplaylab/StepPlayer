using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Loads audio and image assets from StreamingAssets with strict file rules.
public sealed class StreamingAssetLoader : MonoBehaviour
{
    public IEnumerator LoadAudioClip(SongMeta meta, Action<AudioClip> onLoaded)
    {
        if (meta == null || string.IsNullOrWhiteSpace(meta.MusicFile))
        {
            Debug.LogError("Audio load failed: missing #MUSIC in .sm.");
            onLoaded?.Invoke(null);
            yield break;
        }

        var fullPath = Path.Combine(meta.SongDirPath, meta.MusicFile);
        var uri = new Uri(fullPath).AbsoluteUri;

        using var request = UnityWebRequestMultimedia.GetAudioClip(uri, meta.MusicAudioType);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Audio load failed: {meta.SmFilePath} tag #MUSIC file '{meta.MusicFile}' ({request.error}).");
            onLoaded?.Invoke(null);
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(request);
        onLoaded?.Invoke(clip);
    }

    public IEnumerator LoadTexture(string smFilePath, string tag, string songDirPath, string fileName, Action<Texture2D> onLoaded)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            onLoaded?.Invoke(null);
            yield break;
        }

        var fullPath = Path.Combine(songDirPath, fileName);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Image file missing: {smFilePath} tag {tag} file '{fileName}'.");
            onLoaded?.Invoke(null);
            yield break;
        }

        var uri = new Uri(fullPath).AbsoluteUri;
        using var request = UnityWebRequest.Get(uri);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Image load failed: {smFilePath} tag {tag} file '{fileName}' ({request.error}).");
            onLoaded?.Invoke(null);
            yield break;
        }

        var data = request.downloadHandler.data;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(data))
        {
            Debug.LogError($"Image decode failed: {smFilePath} tag {tag} file '{fileName}'.");
            onLoaded?.Invoke(null);
            yield break;
        }

        onLoaded?.Invoke(texture);
    }
}
