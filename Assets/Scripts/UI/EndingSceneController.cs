using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EndingSceneController : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingBodyText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Image endingBackgroundImage;

    [Header("Text Background")]
    [SerializeField] private Image textBackground;
    [SerializeField] private Color textBackgroundColor = new(1f, 1f, 1f, 0.8f);
    [SerializeField] private Vector2 textBackgroundPadding = new(20f, 12f);
    private Vector2 lastTextSize;
    private string lastBackgroundText;
    private bool refreshTextBackground = true;

    private void OnValidate() => refreshTextBackground = true;

    [Header("Audio")]
    [SerializeField, Tooltip("Used for both GOOD and PERFECT endings.")]
    private AudioClip goodEndingBgm;
    [SerializeField] private AudioClip badEndingBgm;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.65f;

    private readonly List<string> lines = new();
    private int lineIndex;
    private int lastAdvanceFrame;
    private bool ready;
    private bool returning;

    private void Start()
    {
        if (GameSession.Ending != null)
            GameAudioManager.PlayBgm(GameSession.Ending.EndingType == EndingType.Bad
                ? badEndingBgm : goodEndingBgm, bgmVolume);
        else
            GameAudioManager.StopBgm();
        if (uiRoot == null || endingBodyText == null)
        {
            Debug.LogError("EndingScene UI is not assigned.", this);
            return;
        }
        SceneUiFactory.ApplyJapaneseFont(uiRoot);
        RefreshResult();
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        // Do not consume the click that entered this scene in the same frame.
        lastAdvanceFrame = Time.frameCount;
        ready = true;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            AdvanceLine();
    }

    private void LateUpdate()
    {
        if (endingBodyText == null) return;
        if (refreshTextBackground || textBackground == null || lastBackgroundText != endingBodyText.text ||
            lastTextSize != endingBodyText.rectTransform.rect.size || endingBodyText.havePropertiesChanged)
            RefreshTextBackground();
        if (textBackground != null) textBackground.color = textBackgroundColor;
    }

    private void RefreshTextBackground()
    {
        RectTransform textRect = endingBodyText.rectTransform;
        if (textBackground == null)
        {
            var background = new GameObject("EndingTextBackground", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(textRect.parent, false);
            textBackground = background.GetComponent<Image>();
            var layout = background.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }
        if (textBackground.transform.parent != textRect.parent)
            textBackground.transform.SetParent(textRect.parent, false);
        int textIndex = textRect.GetSiblingIndex();
        int backgroundIndex = textBackground.transform.GetSiblingIndex();
        textBackground.transform.SetSiblingIndex(backgroundIndex < textIndex ? textIndex - 1 : textIndex);
        textBackground.raycastTarget = false;
        textBackground.color = textBackgroundColor;
        endingBodyText.ForceMeshUpdate();
        TMP_TextInfo info = endingBodyText.textInfo;
        int lineCount = Mathf.Max(1, info.lineCount);
        float centerY = textRect.rect.center.y;
        float height = endingBodyText.preferredHeight;
        if (info.lineCount > 0)
        {
            // Use a full line box for every line, including short final lines.
            float lineHeight = info.lineInfo[0].lineHeight;
            height = Mathf.Max(height, lineHeight * lineCount);
            centerY = (info.lineInfo[0].ascender + info.lineInfo[info.lineCount - 1].descender) * 0.5f;
        }
        RectTransform backgroundRect = textBackground.rectTransform;
        backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.localRotation = textRect.localRotation;
        backgroundRect.localScale = textRect.localScale;
        backgroundRect.position = textRect.TransformPoint(new Vector3(textRect.rect.center.x, centerY, 0f));
        backgroundRect.sizeDelta = new Vector2(textRect.rect.width + Mathf.Max(0f, textBackgroundPadding.x) * 2f,
            height + Mathf.Max(0f, textBackgroundPadding.y) * 2f);
        lastTextSize = textRect.rect.size;
        lastBackgroundText = endingBodyText.text;
        refreshTextBackground = false;
    }

    public void AdvanceLine()
    {
        if (!ready || returning || lastAdvanceFrame == Time.frameCount) return;
        lastAdvanceFrame = Time.frameCount;
        if (lineIndex + 1 >= lines.Count)
        {
            Restart();
            return;
        }

        lineIndex++;
        endingBodyText.text = lines[lineIndex];
    }

    public void ConfigureView(RectTransform root, TMP_Text title, TMP_Text body, Button retry, Image background = null)
    {
        uiRoot = root;
        endingTitleText = title;
        endingBodyText = body;
        retryButton = retry;
        endingBackgroundImage = background;
        SetPreview();
    }

    private void RefreshResult()
    {
        EndingData ending = GameSession.Ending;
        if (endingBackgroundImage != null)
        {
            endingBackgroundImage.sprite = ending != null ? ending.BackgroundImage : null;
            endingBackgroundImage.enabled = endingBackgroundImage.sprite != null;
            endingBackgroundImage.raycastTarget = false;
        }
        if (endingTitleText != null)
            endingTitleText.text = ending != null ? ending.EndingTitle : "NO RESULT";

        lines.Clear();
        if (ending != null)
        {
            if (ending.EndingLines != null)
            {
                foreach (string line in ending.EndingLines)
                    if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
            }

            if (lines.Count == 0 && !string.IsNullOrWhiteSpace(ending.EndingText))
            {
                string[] legacyLines = ending.EndingText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in legacyLines)
                    if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
            }
        }

        if (lines.Count == 0) lines.Add("エンディング本文は現在準備中です。");
        lineIndex = 0;
        endingBodyText.text = lines[0];
    }

    private void SetPreview()
    {
        endingTitleText.text = "ENDING TITLE";
        endingBodyText.text = "EndingData の本文がここに表示されます。";
    }

    private void Restart()
    {
        if (returning) return;
        returning = true;
        GameSession.Reset();
        SceneManager.LoadScene("MatchingScene");
    }
}
