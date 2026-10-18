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
