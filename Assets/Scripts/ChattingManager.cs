using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChattingManager : MonoBehaviour
{
    [Header("Dynamic Chat UI")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private ChatMessageView messagePrefab;
    [SerializeField] private bool clearOnAwake = true;
    [SerializeField, Min(0f)] private float messageSpacing = 8f;

    private readonly List<ChatMessageView> messages = new();
    private Coroutine scrollRoutine;

    public IReadOnlyList<ChatMessageView> Messages => messages;

    private void Awake()
    {
        if (content == null && scrollRect != null)
            content = scrollRect.content;

        ConfigureContentLayout();

        if (clearOnAwake)
            ClearMessages();
    }

    public void AddChat(string playerMessage, string npcMessage)
    {
        if (!string.IsNullOrWhiteSpace(playerMessage))
            AddMessage(playerMessage, true);

        if (!string.IsNullOrWhiteSpace(npcMessage))
            AddMessage(npcMessage, false);
    }

    public ChatMessageView AddPlayerMessage(string message) => AddMessage(message, true);
    public ChatMessageView AddNpcMessage(string message) => AddMessage(message, false);

    public ChatMessageView AddMessage(string message, bool isPlayer)
    {
        if (string.IsNullOrWhiteSpace(message) || content == null || messagePrefab == null)
            return null;

        ChatMessageView view = Instantiate(messagePrefab, content);
        view.SetMessage(message, isPlayer);
        messages.Add(view);
        ScheduleScrollToBottom();
        return view;
    }

    public void ClearMessages()
    {
        messages.Clear();

        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject child = content.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    // Retained for existing UnityEvent bindings. Dynamic chat follows the latest message.
    public void ScrollToStep(int step) => ScheduleScrollToBottom();

    public void ScrollToBottomImmediate()
    {
        if (scrollRect == null || content == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void ConfigureContentLayout()
    {
        if (content == null)
            return;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = messageSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ScheduleScrollToBottom()
    {
        if (!isActiveAndEnabled)
            return;

        if (scrollRoutine != null)
            StopCoroutine(scrollRoutine);

        scrollRoutine = StartCoroutine(ScrollToBottomAfterLayout());
    }

    private IEnumerator ScrollToBottomAfterLayout()
    {
        yield return null;
        ScrollToBottomImmediate();
        scrollRoutine = null;
    }
}
