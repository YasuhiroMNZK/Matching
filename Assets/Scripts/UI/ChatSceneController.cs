using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ChatSceneController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterData fallbackCharacter;
    [SerializeField, TextArea(2, 4)] private string incompleteInvitationText = "……すまない。今は、汝の誘いに応えることはできない。";
    [SerializeField, Min(0f)] private float invitationReplyDelay = 1f;
    [SerializeField, Min(0f)] private float returnDelay = 2.5f;
    [SerializeField, Min(0f)] private float endingTransitionDelay = 2.5f;

    [Header("Scene UI")]
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private ScrollRect messageScroll;
    [SerializeField] private RectTransform messageContent;
    [SerializeField] private ChatMessageView messagePrefab;
    [SerializeField] private Button aizuchiButton;
    [SerializeField] private Button deepDiveButton;
    [SerializeField] private Button smallTalkButton;

    [Header("Invitation UI")]
    [SerializeField] private GameObject invitationOverlay;
    [SerializeField] private GameObject panelList;
    [SerializeField] private RectTransform invitationWordList;
    [SerializeField] private TMP_Text invitationHeadingText;
    [SerializeField] private Button invitationDecideButton;

    private ChatManager chat;
    private HonestyManager honesty;
    private WordManager words;
    private readonly List<Button> wordButtons = new();
    private bool transitioning;

    private void Start()
    {
        CharacterData character = GameSession.Character != null ? GameSession.Character : fallbackCharacter;
        if (character == null || uiRoot == null)
        {
            Debug.LogError("ChatScene UI or CharacterData is not assigned.", this);
            return;
        }
        if (GameSession.Character == null) GameSession.SelectCharacter(character);

        SceneUiFactory.ApplyJapaneseFont(uiRoot);
        honesty = gameObject.AddComponent<HonestyManager>();
        words = gameObject.AddComponent<WordManager>();
        chat = gameObject.AddComponent<ChatManager>();
        chat.Configure(character, honesty, words);

        invitationOverlay.SetActive(false);
        ClearDynamicChildren(messageContent);
        ClearDynamicChildren(invitationWordList);
        BindEvents();
        chat.StartChat(character);
    }

    public void ConfigureView(CharacterData character, RectTransform root, ScrollRect scroll,
        RectTransform content, ChatMessageView bubblePrefab,
        Button aizuchi, Button deepDive, Button smallTalk, GameObject inviteOverlay,
        RectTransform wordList, TMP_Text inviteHeading, Button decide)
    {
        fallbackCharacter = character;
        uiRoot = root;
        messageScroll = scroll;
        messageContent = content;
        messagePrefab = bubblePrefab;
        aizuchiButton = aizuchi;
        deepDiveButton = deepDive;
        smallTalkButton = smallTalk;
        invitationOverlay = inviteOverlay;
        invitationWordList = wordList;
        invitationHeadingText = inviteHeading;
        invitationDecideButton = decide;
        invitationOverlay.SetActive(false);
    }

    private void BindEvents()
    {
        chat.OnReply.AddListener(OnReply);
        chat.OnDateInvitationRequired.AddListener(OnInvitationRequired);
    }

    private void OnReply(string player, string npc)
    {
        AddMessage(player, true);
        AddMessage(npc, false);
    }

    public void ChooseAizuchi() => chat?.ChooseAizuchi();
    public void ChooseDeepDive() => chat?.ChooseDeepDive();
    public void ChooseSmallTalk() => chat?.ChooseSmallTalk();

    private void AddMessage(string value, bool player)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (messagePrefab == null)
        {
            Debug.LogError("MessageBubble prefab is not assigned.", this);
            return;
        }

        ChatMessageView bubble = Instantiate(messagePrefab, messageContent);
        bubble.name = player ? "PlayerMessage" : "CharacterMessage";
        SceneUiFactory.ApplyJapaneseFont(bubble.transform);
        Canvas.ForceUpdateCanvases();
        VerticalLayoutGroup contentLayout = messageContent.GetComponent<VerticalLayoutGroup>();
        float availableWidth = messageContent.rect.width - (contentLayout != null ? contentLayout.padding.horizontal : 0f);
        if (availableWidth <= 0f) availableWidth = float.PositiveInfinity;
        bubble.SetMessage(value, player, chat.Character.IconImage, availableWidth);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContent);
        messageScroll.verticalNormalizedPosition = 0f;
    }

    private void OnInvitationRequired()
    {
        SetConversationButtons(false);
        ShowInvitation();
    }

    private void ShowInvitation()
    {
        if (transitioning) return;
        if (!words.HasInvitationWords)
        {
            if (chat.State != ChatState.DateInvitation)
            {
                return;
            }
            StartCoroutine(FailInvitationAndReturn());
            return;
        }
        if (chat.State == ChatState.AwaitingCommand && !chat.InviteToDate()) return;

        ClearWordButtons();
        for (int i = 0; i < words.AcquiredWords.Count; i++)
        {
            WordData word = words.AcquiredWords[i].Data;
            if (word.Genre == WordGenre.Weakness) continue;
            Button button = SceneUiFactory.Button("Word_" + word.WordId, invitationWordList,
                $"{word.Genre}　{word.DisplayText}", SceneUiFactory.Muted);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 78f;
            button.onClick.AddListener(() => { words.TrySelect(word); RefreshInvitationHeading(); });
            wordButtons.Add(button);
        }
        RefreshInvitationHeading();
        invitationOverlay.SetActive(true);
    }

    private void RefreshInvitationHeading()
    {
        invitationHeadingText.text = $"{words.SelectedPlace?.DisplayText ?? "---"}に行って、{words.SelectedAction?.DisplayText ?? "---"}。{words.SelectedPurpose?.DisplayText ?? "---"}のを楽しもう！";
        invitationDecideButton.gameObject.SetActive(words.IsInvitationComplete);
    }

    public void CompleteInvitation()
    {
        if (transitioning) return;
        if (!words.IsInvitationComplete)
        {
            return;
        }
        StartCoroutine(CompleteInvitationAndShowEnding());
    }

    private IEnumerator FailInvitationAndReturn()
    {
        transitioning = true;
        SetConversationButtons(false);
        yield return new WaitForSeconds(invitationReplyDelay);
        AddMessage(incompleteInvitationText, false);
        yield return new WaitForSeconds(returnDelay);
        GameSession.Reset();
        SceneManager.LoadScene("MatchingScene");
    }

    private IEnumerator CompleteInvitationAndShowEnding()
    {
        transitioning = true;
        panelList.SetActive(false);
        SetConversationButtons(false);
        EndingData ending = chat.CompleteInvitation();
        EndingType endingType = ending != null ? ending.EndingType : EndingType.Bad;
        string reaction = chat.Character.GetEndingText(endingType);
        yield return new WaitForSeconds(invitationReplyDelay);
        if (!string.IsNullOrWhiteSpace(reaction)) AddMessage(reaction, false);
        GameSession.Complete(ending, endingType, honesty.Current, words.AcquiredWords.Count);
        yield return new WaitForSeconds(endingTransitionDelay);
        SceneManager.LoadScene("EndingScene");
    }

    private void SetConversationButtons(bool interactable)
    {
        aizuchiButton.interactable = interactable;
        deepDiveButton.interactable = interactable;
        smallTalkButton.interactable = interactable;
    }

    private void ClearWordButtons()
    {
        for (int i = 0; i < wordButtons.Count; i++)
            if (wordButtons[i] != null) Destroy(wordButtons[i].gameObject);
        wordButtons.Clear();
    }

    private static void ClearDynamicChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
