using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SongSelectController : MonoBehaviour
{
    [SerializeField] StreamingAssetLoader loader;

    [Header("UI")]
    [SerializeField] RectTransform listRoot;
    [SerializeField] SongRowView rowPrefab;

    [Header("Sound Effects")]
    [SerializeField] AudioSource seSource;
    [SerializeField] AudioClip moveSe;
    [SerializeField] AudioClip decideSe;

    [Header("Preview")]
    [SerializeField] AudioSource previewSource;
    [SerializeField] float previewStartTimeSec = 0f;
    [SerializeField] float previewFadeOutSeconds = 0.15f;

    [Header("Preview Preload (nearby)")]
    [SerializeField] int preloadRadius = 1;

    [Header("Reel Layout")]
    [SerializeField] float centerX = -20f;
    [SerializeField] float centerY = 0f;
    [SerializeField] float spacingY = 110f;

    [SerializeField] float rightXMax = 80f;
    [SerializeField] AnimationCurve xOffsetByAbsIndex = AnimationCurve.EaseInOut(0, 0, 6, 1);

    [Header("Reel Motion")]
    [SerializeField] float smoothTime = 0.08f;
    [SerializeField] float snapThreshold = 0.1f;

    [Header("Reel Visual")]
    [SerializeField] AnimationCurve scaleByAbsIndex = AnimationCurve.EaseInOut(0, 1.0f, 6, 0.88f);
    [SerializeField] AnimationCurve alphaByAbsIndex = AnimationCurve.EaseInOut(0, 1.0f, 6, 0.35f);

    readonly List<SongRowView> rows = new();
    readonly List<Vector2> rowVels = new();
    readonly List<RectTransform> rowRects = new();
    readonly List<CanvasGroup> rowCgs = new();

    int selectedIndex = 0;
    bool isTransitioning = false;
    Coroutine previewCoroutine;

    List<SongMeta> songs = new();

    IEnumerator Start()
    {
        yield return BuildCatalogAndLoadAssets();
        BuildList();
        PreloadNearbyPreviewAudio(selectedIndex);
        PlayPreview();
    }

    void Update()
    {
        if (isTransitioning) return;

        if (KeyBindings.MenuUpPressedThisFrame())
            MoveSelection(-1);

        if (KeyBindings.MenuDownPressedThisFrame())
            MoveSelection(+1);

        if (KeyBindings.MenuConfirmPressedThisFrame())
            StartCoroutine(SelectSongAndLoad(selectedIndex));

        ApplyReelLayout();
    }

    IEnumerator BuildCatalogAndLoadAssets()
    {
        songs = SongCatalog.BuildCatalog();

        if (loader == null)
            loader = GetComponent<StreamingAssetLoader>();
        if (loader == null)
            loader = gameObject.AddComponent<StreamingAssetLoader>();

        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            yield return loader.LoadAudioClip(song, clip => song.MusicClip = clip);

            if (!string.IsNullOrWhiteSpace(song.BannerFile))
                yield return loader.LoadTexture(song.SmFilePath, "#BANNER", song.SongDirPath, song.BannerFile, tex => song.BannerTexture = tex);

            if (!string.IsNullOrWhiteSpace(song.BackgroundFile))
                yield return loader.LoadTexture(song.SmFilePath, "#BACKGROUND", song.SongDirPath, song.BackgroundFile, tex => song.BackgroundTexture = tex);
        }
    }

    void BuildList()
    {
        rows.Clear();
        rowRects.Clear();
        rowCgs.Clear();

        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);

        for (int i = 0; i < songs.Count; i++)
        {
            var row = Instantiate(rowPrefab, listRoot);
            row.Bind(this, i, songs[i]);

            rows.Add(row);

            var rt = (RectTransform)row.transform;
            rowRects.Add(rt);

            if (!rt.TryGetComponent(out CanvasGroup cg))
                cg = rt.gameObject.AddComponent<CanvasGroup>();
            rowCgs.Add(cg);
        }

        rowVels.Clear();
        for (int i = 0; i < rows.Count; i++)
            rowVels.Add(Vector2.zero);
    }

    void MoveSelection(int delta)
    {
        int prevIndex = selectedIndex;
        selectedIndex = Mathf.Clamp(selectedIndex + delta, 0, rows.Count - 1);

        if (selectedIndex != prevIndex)
        {
            PlayMoveSe();
            PreloadNearbyPreviewAudio(selectedIndex);
            PlayPreview();
        }
    }

    void ApplyReelLayout()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        int visualSelected = 0;
        float best = float.MaxValue;

        for (int i = 0; i < rows.Count; i++)
        {
            var rect = rowRects[i];

            int d = i - selectedIndex;
            float abs = Mathf.Abs(d);

            float x01 = xOffsetByAbsIndex.Evaluate(abs);
            float x = Mathf.Lerp(centerX, rightXMax, x01);
            float y = centerY - d * spacingY;

            var target = new Vector2(x, y);

            var v = rowVels[i];
            var p = Vector2.SmoothDamp(rect.anchoredPosition, target, ref v, smoothTime, Mathf.Infinity, dt);

            if ((p - target).sqrMagnitude < snapThreshold * snapThreshold)
                p = target;

            rect.anchoredPosition = p;
            rowVels[i] = v;

            rect.localScale = Vector3.one * scaleByAbsIndex.Evaluate(abs);
            rowCgs[i].alpha = alphaByAbsIndex.Evaluate(abs);

            float dist = Mathf.Abs(rect.anchoredPosition.y - centerY);
            if (dist < best)
            {
                best = dist;
                visualSelected = i;
            }
        }

        for (int i = 0; i < rows.Count; i++)
            rows[i].SetSelected(i == visualSelected);
    }

    public void OnRowClicked(int index)
    {
        if (isTransitioning) return;

        selectedIndex = index;

        PreloadNearbyPreviewAudio(selectedIndex);
        PlayPreview();

        StartCoroutine(SelectSongAndLoad(index));
    }

    IEnumerator SelectSongAndLoad(int index)
    {
        isTransitioning = true;

        StopPreview();

        var song = GetSong(index);
        if (song == null)
        {
            isTransitioning = false;
            yield break;
        }

        SelectedSong.Value = song;

        PlayDecideSe();

        if (decideSe != null)
            yield return new WaitForSecondsRealtime(decideSe.length);

        SceneManager.LoadScene("Play");
    }

    void PlayMoveSe()
    {
        if (seSource != null && moveSe != null)
            seSource.PlayOneShot(moveSe, 0.8f);
    }

    void PlayDecideSe()
    {
        if (seSource != null && decideSe != null)
            seSource.PlayOneShot(decideSe, 2.0f);
    }

    void PlayPreview()
    {
        if (previewCoroutine != null)
            StopCoroutine(previewCoroutine);

        var song = GetSong(selectedIndex);
        if (song == null || previewSource == null || song.MusicClip == null)
            return;

        previewCoroutine = StartCoroutine(PlayPreviewCoroutine(song));
    }

    IEnumerator PlayPreviewCoroutine(SongMeta song)
    {
        previewSource.Stop();

        yield return null;

        var clip = song.MusicClip;
        if (clip == null) yield break;

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        while (clip.loadState == AudioDataLoadState.Loading)
            yield return null;

        float startTime = previewStartTimeSec;
        if (song.SampleStart.HasValue)
            startTime = song.SampleStart.Value;

        startTime = Mathf.Clamp(startTime, 0f, clip.length);

        float previewLength = -1f;
        if (song.SampleLength.HasValue)
            previewLength = Mathf.Clamp(song.SampleLength.Value, 0f, clip.length - startTime);

        previewSource.clip = clip;
        previewSource.time = startTime;
        previewSource.loop = previewLength <= 0f;
        previewSource.Play();

        float baseVolume = previewSource.volume;

        if (previewLength > 0f)
        {
            float endTime = startTime + previewLength;
            while (previewSource != null && previewSource.clip == clip)
            {
                if (!previewSource.isPlaying)
                {
                    previewSource.volume = baseVolume;
                    previewSource.Play();
                }

                if (previewFadeOutSeconds > 0f)
                {
                    float remaining = endTime - previewSource.time;
                    if (remaining <= previewFadeOutSeconds)
                        previewSource.volume = baseVolume * Mathf.Clamp01(remaining / previewFadeOutSeconds);
                    else
                        previewSource.volume = baseVolume;
                }

                if (previewSource.time >= endTime)
                {
                    previewSource.time = startTime;
                    previewSource.volume = baseVolume;
                }

                yield return null;
            }
        }
        else if (previewSource != null)
        {
            previewSource.volume = baseVolume;
        }
    }

    void StopPreview()
    {
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }

        if (previewSource != null)
            previewSource.Stop();
    }

    void PreloadNearbyPreviewAudio(int centerIndex)
    {
        if (preloadRadius <= 0)
        {
            PreloadPreviewAudio(centerIndex);
            return;
        }

        int start = Mathf.Max(0, centerIndex - preloadRadius);
        int end = Mathf.Min(songs.Count - 1, centerIndex + preloadRadius);

        for (int i = start; i <= end; i++)
            PreloadPreviewAudio(i);
    }

    void PreloadPreviewAudio(int index)
    {
        if (index < 0 || index >= songs.Count) return;

        var song = GetSong(index);
        if (song == null || song.MusicClip == null) return;

        var clip = song.MusicClip;

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
    }

    SongMeta GetSong(int index)
    {
        if (songs == null || songs.Count == 0) return null;
        index = Mathf.Clamp(index, 0, songs.Count - 1);
        return songs[index];
    }
}
