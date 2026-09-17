using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EndingSceneController : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingBodyText;
    [SerializeField] private Button retryButton;

    private void Start()
    {
        if (uiRoot == null)
        {
            Debug.LogError("EndingScene UI is not assigned.", this);
            return;
        }
        SceneUiFactory.ApplyJapaneseFont(uiRoot);
        RefreshResult();
        retryButton.onClick.AddListener(Restart);
    }

    public void ConfigureView(RectTransform root, TMP_Text title, TMP_Text body, Button retry)
    {
        uiRoot = root;
        endingTitleText = title;
        endingBodyText = body;
        retryButton = retry;
        SetPreview();
    }

    private void RefreshResult()
    {
        EndingData ending = GameSession.Ending;
        endingTitleText.text = ending != null ? ending.EndingTitle : "NO RESULT";
        endingBodyText.text = ending != null && !string.IsNullOrWhiteSpace(ending.EndingText)
            ? ending.EndingText : "エンディング本文は現在準備中です。";
    }

    private void SetPreview()
    {
        endingTitleText.text = "ENDING TITLE";
        endingBodyText.text = "EndingData の本文がここに表示されます。";
    }

    private void Restart()
    {
        GameSession.Reset();
        SceneManager.LoadScene("MatchingScene");
    }
}
