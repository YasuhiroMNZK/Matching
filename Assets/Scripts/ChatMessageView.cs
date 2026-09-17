using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatMessageView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image bubbleBackground;
    [SerializeField] private Image characterIcon;
    [SerializeField] private LayoutElement rowLayout;
    [SerializeField] private LayoutElement bubbleLayout;
    [SerializeField] private LayoutElement leftSpacer;
    [SerializeField] private LayoutElement rightSpacer;

    [Header("Appearance")]
    [SerializeField] private Sprite playerBubbleSprite;
    [SerializeField] private Sprite npcBubbleSprite;
    [SerializeField] private Color playerTextColor = Color.white;
    [SerializeField] private Color npcTextColor = new(0.12f, 0.12f, 0.15f, 1f);
    [SerializeField, Min(40f)] private float maxBubbleWidth = 160f;
    [SerializeField, Tooltip("X = left inset, Y = right inset")]
    private Vector2 playerHorizontalPadding = new(12f, 12f);
    [SerializeField, Tooltip("X = left inset, Y = right inset")]
    private Vector2 npcHorizontalPadding = new(12f, 12f);
    [SerializeField, Min(0f)] private float verticalPadding = 8f;

    public string Message { get; private set; }
    public bool IsPlayer { get; private set; }

    private void Awake()
    {
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset();
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    public void SetMessage(string message, bool isPlayer, Sprite npcIcon = null, float availableRowWidth = float.PositiveInfinity)
    {
        Message = message ?? string.Empty;
        IsPlayer = isPlayer;

        messageText.text = Message;
        messageText.color = isPlayer ? playerTextColor : npcTextColor;
        bubbleBackground.sprite = isPlayer ? playerBubbleSprite : npcBubbleSprite;
        bubbleBackground.color = Color.white;

        if (characterIcon != null)
        {
            characterIcon.sprite = npcIcon;
            characterIcon.gameObject.SetActive(!isPlayer && npcIcon != null);
        }

        leftSpacer.flexibleWidth = isPlayer ? 1f : 0f;
        rightSpacer.flexibleWidth = isPlayer ? 0f : 1f;

        Vector2 horizontalPadding = isPlayer ? playerHorizontalPadding : npcHorizontalPadding;
        float leftPadding = Mathf.Max(0f, horizontalPadding.x);
        float rightPadding = Mathf.Max(0f, horizontalPadding.y);
        float topBottomPadding = Mathf.Max(0f, verticalPadding);
        float iconWidth = characterIcon != null && characterIcon.gameObject.activeSelf
            ? Mathf.Max(0f, characterIcon.GetComponent<LayoutElement>()?.preferredWidth ?? characterIcon.rectTransform.rect.width)
            : 0f;
        float bubbleWidthLimit = float.IsInfinity(availableRowWidth)
            ? maxBubbleWidth
            : Mathf.Min(maxBubbleWidth, Mathf.Max(1f, availableRowWidth - iconWidth));
        RectTransform textRect = messageText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(leftPadding, topBottomPadding);
        textRect.offsetMax = new Vector2(-rightPadding, -topBottomPadding);

        // First measure without automatic wrapping. Measuring at the maximum width
        // and then shrinking to the longest wrapped line can trigger a second wrap.
        float unwrappedWidth = messageText.GetPreferredValues(Message).x;
        float bubbleWidth = Mathf.Min(bubbleWidthLimit, Mathf.Ceil(unwrappedWidth + leftPadding + rightPadding + 1f));
        float actualTextWidth = Mathf.Max(1f, bubbleWidth - leftPadding - rightPadding);
        float textHeight = messageText.GetPreferredValues(Message, actualTextWidth, 0f).y;
        float bubbleHeight = textHeight + topBottomPadding * 2f;

        bubbleLayout.preferredWidth = bubbleWidth;
        bubbleLayout.preferredHeight = bubbleHeight;
        rowLayout.preferredHeight = Mathf.Max(bubbleHeight, characterIcon != null && characterIcon.gameObject.activeSelf
            ? characterIcon.rectTransform.rect.height : 0f);

        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
    }
}
