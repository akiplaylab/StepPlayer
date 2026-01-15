using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlaySceneSongInfoPresenter : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text difficultyText;

    void Awake()
    {
        EnsureRuntimeUi();
    }

    public void SetSong(SongMeta song, ChartDifficulty difficulty)
    {
        if (song == null) return;

        EnsureRuntimeUi();
        titleText.text = song.DisplayTitle;
        difficultyText.text = $"{difficulty.ToString().ToUpperInvariant()}";
    }

    void EnsureRuntimeUi()
    {
        if (titleText != null && difficultyText != null)
            return;

        var canvasObject = new GameObject("PlaySceneSongInfoCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("SongInfoPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasObject.transform, false);

        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.offsetMin = new Vector2(32, 16);
        panelRect.offsetMax = new Vector2(-32, 96);

        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.spacing = 24f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        titleText = CreateText("SongTitle", panel.transform, 28f, FontStyles.Bold);
        difficultyText = CreateText("SongDifficulty", panel.transform, 20f, FontStyles.Bold);
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
}
