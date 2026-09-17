using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SceneUiFactory
{
    public static readonly Color Background = new(0.055f, 0.045f, 0.075f, 1f);
    public static readonly Color Panel = new(0.12f, 0.09f, 0.15f, 0.98f);
    public static readonly Color Accent = new(0.96f, 0.25f, 0.5f, 1f);
    public static readonly Color Muted = new(0.35f, 0.31f, 0.39f, 1f);
    private static TMP_FontAsset japaneseFont;
    private static bool fontResolved;

    public static RectTransform CreateCanvas(string name = "Canvas")
    {
        GameObject cameraObject = new("UICamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.orthographic = true;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject canvasObject = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject.GetComponent<RectTransform>();
    }

    public static RectTransform PanelObject(string name, Transform parent, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    public static TMP_Text Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        if (Application.isPlaying)
        {
            TMP_FontAsset font = GetJapaneseFont();
            if (font != null)
                text.font = font;
        }
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = true;
        return text;
    }

    public static void ApplyJapaneseFont(Transform root)
    {
        TMP_FontAsset font = GetJapaneseFont();
        if (font == null || root == null)
            return;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            texts[i].font = font;
    }

    private static TMP_FontAsset GetJapaneseFont()
    {
        if (fontResolved)
            return japaneseFont;

        fontResolved = true;
        string[] preferredFonts =
        {
            "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo", "MS Gothic",
            "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Noto Sans CJK JP", "Noto Sans JP"
        };

        string[] installed = Font.GetOSInstalledFontNames();
        for (int i = 0; i < preferredFonts.Length; i++)
        {
            for (int j = 0; j < installed.Length; j++)
            {
                if (!string.Equals(installed[j], preferredFonts[i], System.StringComparison.OrdinalIgnoreCase))
                    continue;

                japaneseFont = TMP_FontAsset.CreateFontAsset(installed[j], "Regular", 48);
                if (japaneseFont == null)
                    japaneseFont = TMP_FontAsset.CreateFontAsset(installed[j], "Normal", 48);

                if (japaneseFont != null)
                {
                    japaneseFont.name = "Runtime Japanese UI Font";
                    return japaneseFont;
                }
            }
        }

        Debug.LogWarning("No Japanese OS font was found. Japanese UI text may display as squares.");
        return null;
    }

    public static Button Button(string name, Transform parent, string label, Color color)
    {
        RectTransform rect = PanelObject(name, parent, color);
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        button.colors = colors;
        TMP_Text text = Text("Label", rect, label, 34f);
        Stretch(text.rectTransform, 16f, 8f, 16f, 8f);
        return button;
    }

    public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
