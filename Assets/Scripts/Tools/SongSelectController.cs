using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SongSelectController : MonoBehaviour
{
    [SerializeField] SongLibrary library;

    [Header("UI")]
    [SerializeField] Transform listRoot;
    [SerializeField] SongRowView rowPrefab;

    [Header("Sound Effects")]
    [SerializeField] AudioSource seSource;
    [SerializeField] AudioClip moveSe;
    [SerializeField] AudioClip decideSe;
    [SerializeField] float decideDelaySec = 0.12f;

    [Header("Preview")]
    [SerializeField] AudioSource previewSource;
    [SerializeField] float previewStartTimeSec = 0f;

    readonly List<SongRowView> rows = new();
    int selectedIndex = 0;
    bool isTransitioning = false;
    Coroutine previewCoroutine;

    void Start()
    {
        BuildList();
        UpdateSelection();
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
    }

    void BuildList()
    {
        rows.Clear();

        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);

        for (int i = 0; i < library.Count; i++)
        {
            var row = Instantiate(rowPrefab, listRoot);
            row.Bind(this, i, library.Songs[i]);
            rows.Add(row);
        }
    }

    void MoveSelection(int delta)
    {
        int prevIndex = selectedIndex;
        selectedIndex = Mathf.Clamp(selectedIndex + delta, 0, rows.Count - 1);

        if (selectedIndex != prevIndex)
        {
            UpdateSelection();
            PlayMoveSe();
            PlayPreview();
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < rows.Count; i++)
            rows[i].SetSelected(i == selectedIndex);
    }

    public void OnRowClicked(int index)
    {
        if (isTransitioning) return;

        selectedIndex = index;
        UpdateSelection();
        PlayPreview();
        StartCoroutine(SelectSongAndLoad(index));
    }

    IEnumerator SelectSongAndLoad(int index)
    {
        isTransitioning = true;

        StopPreview();

        var song = library.Get(index);
        if (song == null)
        {
            isTransitioning = false;
            yield break;
        }

        SelectedSong.Value = song;

        PlayDecideSe();

        if (decideSe != null)
            yield return new WaitForSecondsRealtime(decideSe.length);

        SceneManager.LoadScene("GameScene");
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

        var song = library.Get(selectedIndex);
        if (song == null || previewSource == null || song.musicClip == null)
            return;

        previewCoroutine = StartCoroutine(PlayPreviewCoroutine(song));
    }

    IEnumerator PlayPreviewCoroutine(SongDefinition song)
    {
        previewSource.Stop();

        if (!song.musicClip.preloadAudioData)
        {
            song.musicClip.LoadAudioData();

            while (song.musicClip.loadState == AudioDataLoadState.Loading)
                yield return null;
        }

        previewSource.clip = song.musicClip;
        previewSource.time = Mathf.Clamp(previewStartTimeSec, 0f, song.musicClip.length);
        previewSource.Play();
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
}
