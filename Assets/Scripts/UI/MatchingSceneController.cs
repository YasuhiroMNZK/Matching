using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MatchingSceneController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterData character;
    [SerializeField, Tooltip("Characters available this visit. Shuffled once when the scene starts; empty entries and duplicates are ignored. Falls back to Character when empty.")]
    private List<CharacterData> characterPool = new();

    private readonly List<CharacterData> characterOrder = new();
    private readonly Dictionary<CharacterData, List<TagData>> displayedTags = new();
    private int characterIndex;

    [Header("Scene UI")]
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text profileDetailsText;
    [SerializeField] private Button passButton;
    [SerializeField] private Button likeButton;

    [Header("Audio")]
    [SerializeField] private AudioClip bgm;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.65f;
    [SerializeField, Tooltip("SE1: release without changing character.")]
    private AudioClip passCancelledSe;
    [SerializeField, Tooltip("SE2: start changing character.")]
    private AudioClip passChangedSe;
    [SerializeField, Tooltip("Play once when Like is pressed.")]
    private AudioClip likeSe;
    [SerializeField, Range(0f, 1f)] private float seVolume = 1f;

    [Header("Scene entry")]
    [SerializeField, Min(0f)] private float entryInputDelay = 1f;
    private bool entryInputLocked;
    private Coroutine entryInputRoutine;

    [Header("Portrait transition")]
    [SerializeField, Tooltip("Only portraits inside this rectangle are visible. Resize this RectTransform to set the slide area.")]
    private RectTransform portraitViewport;
    [SerializeField, Min(0f)] private float portraitSlideDuration = 0.35f;

    [Header("Pass drag")]
    [SerializeField, Min(1f), Tooltip("Horizontal mouse displacement in screen pixels required to pass.")]
    private float passDragThreshold = 30f;
    [SerializeField] private GameObject leftDragHint;
    [SerializeField] private GameObject rightDragHint;
    [SerializeField] private UnityEvent onLeftDragHint = new();
    [SerializeField] private UnityEvent onRightDragHint = new();
    [SerializeField] private UnityEvent onDragHintCleared = new();

    [System.Serializable]
    private sealed class DragHintVisuals
    {
        public GameObject idleImage;
        public GameObject heldAnimation;
        public GameObject releasedImage;

        public bool IsConfigured => idleImage != null || heldAnimation != null || releasedImage != null;
    }

    [Header("Left / right hint states (children of their respective Drag Hint)")]
    [SerializeField] private DragHintVisuals leftDragHintStates = new();
    [SerializeField] private DragHintVisuals rightDragHintStates = new();
    [SerializeField] private UnityEvent onLeftDragReleased = new();
    [SerializeField] private UnityEvent onRightDragReleased = new();

    private enum DragHintState { Idle, Held, Released }

    [Header("Pass press / release objects")]
    [SerializeField] private GameObject passPressedObject;
    [SerializeField] private GameObject passReleasedObject;
    [SerializeField, Tooltip("Optional. When empty, the Animator's default state is played from the start.")]
    private string passReleaseAnimationState;
    private Coroutine releaseObjectRoutine;
    private bool passReleasePlaying;
    private readonly Dictionary<Graphic, bool> hiddenPassGraphics = new();

    [Header("Like press animation")]
    [SerializeField] private GameObject likePressedObject;
    [UnityEngine.Serialization.FormerlySerializedAs("likeReleaseAnimationState")]
    [SerializeField, Tooltip("Optional. When empty, the Animator's default state is played from the start.")]
    private string likeAnimationState;
    [SerializeField, Min(0f)] private float likeSceneDelay = 0.5f;
    private Coroutine likeAnimationRoutine;
    private bool likeAnimationPlaying;
    private readonly Dictionary<Graphic, bool> hiddenLikeGraphics = new();

    private bool holdingPass;
    private Vector2 passPressPosition;
    private int passPointerId;
    private int passDragDirection;

    private Coroutine portraitSlide;
    private Image incomingPortrait;
    private Vector2 portraitRestPosition;
    private bool passWasInteractable;
    private bool likeWasInteractable;

    [Header("Tag slots")]
    [SerializeField] private GameObject[] tagSlots = new GameObject[4];

    private void Start()
    {
        GameAudioManager.PlayBgm(bgm, bgmVolume);
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        BuildCharacterOrder();
        displayedTags.Clear();
        foreach (CharacterData candidate in characterOrder)
            displayedTags.Add(candidate, SelectDisplayTags(candidate));
        if (character == null || uiRoot == null)
        {
            Debug.LogError("MatchingScene UI or CharacterData is not assigned.", this);
            return;
        }

        if (entryInputDelay > 0f)
        {
            entryInputLocked = true;
            LockButtonInteraction();
        }

        SceneUiFactory.ApplyJapaneseFont(uiRoot);
        ConfigurePortraitViewport();
        RefreshCharacter();
        ConfigurePassDrag();
        ConfigureLikeButton();
        if (likeButton == null) Debug.LogError("MatchingScene Like Button is not assigned.", this);
        if (entryInputLocked)
            entryInputRoutine = StartCoroutine(UnlockSceneEntryInput());
    }

    private IEnumerator UnlockSceneEntryInput()
    {
        yield return new WaitForSecondsRealtime(entryInputDelay);
        entryInputLocked = false;
        entryInputRoutine = null;
        RestoreButtonInteraction();
    }

    public void ConfigureView(CharacterData data, RectTransform root, Image portrait, TMP_Text nameText,
        TMP_Text summary, TMP_Text profileDetails, Button pass, Button like)
    {
        character = data;
        uiRoot = root;
        portraitImage = portrait;
        characterNameText = nameText;
        summaryText = summary;
        profileDetailsText = profileDetails;
        passButton = pass;
        likeButton = like;
        RefreshCharacter();
    }

    private void RefreshCharacter()
    {
        if (character == null || portraitImage == null) return;
        portraitImage.sprite = character.ProfileImage;
        portraitImage.preserveAspect = true;
        characterNameText.text = character.CharacterName;
        summaryText.text = $"種族：{character.Species}\n年齢：{character.Age}歳\n性別：{character.Gender}";
        profileDetailsText.text = $"{character.ProfileText}";
        RefreshTags();
    }

    private void BuildCharacterOrder()
    {
        characterOrder.Clear();
        if (characterPool != null)
        {
            foreach (CharacterData candidate in characterPool)
            {
                if (candidate != null && !characterOrder.Contains(candidate))
                    characterOrder.Add(candidate);
            }
        }

        // Preserve existing single-character scenes when no pool is configured.
        if (characterOrder.Count == 0 && character != null)
            characterOrder.Add(character);

        for (int i = characterOrder.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (characterOrder[i], characterOrder[randomIndex]) = (characterOrder[randomIndex], characterOrder[i]);
        }

        characterIndex = 0;
        character = characterOrder.Count > 0 ? characterOrder[0] : null;
    }

    private static List<TagData> SelectDisplayTags(CharacterData data)
    {
        List<TagData> candidates = new();
        HashSet<string> seenNames = new();
        foreach (TagData tag in data.Tags)
        {
            if (tag != null && !string.IsNullOrWhiteSpace(tag.DisplayName) && seenNames.Add(tag.DisplayName))
                candidates.Add(tag);
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
        }

        return candidates.GetRange(0, Mathf.Min(4, candidates.Count));
    }

    private void RefreshTags()
    {
        if (!displayedTags.TryGetValue(character, out List<TagData> candidates))
        {
            // ConfigureView also renders an editor preview before Start runs.
            candidates = SelectDisplayTags(character);
            displayedTags.Add(character, candidates);
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject slot = tagSlots != null && i < tagSlots.Length ? tagSlots[i] : null;
            if (slot == null) continue;

            TMP_Text label = slot.GetComponentInChildren<TMP_Text>(true);
            bool show = i < candidates.Count && label != null;
            if (show) label.text = candidates[i].DisplayName;
            slot.SetActive(show);
        }
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindChildByName(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    public void Pass()
    {
        // Existing Button.onClick bindings must not switch on a simple click.
        // PointerUp normally finishes the gesture before onClick is dispatched.
        FinishPassDrag();
    }

    private void ConfigurePassDrag()
    {
        HidePassFeedback();
        SetDragHintVisuals(0, DragHintState.Idle);
        if (passButton == null) return;
        EventTrigger trigger = passButton.GetComponent<EventTrigger>();
        if (trigger == null) trigger = passButton.gameObject.AddComponent<EventTrigger>();
        AddPassEvent(trigger, EventTriggerType.PointerDown, BeginPassDrag);
        AddPassEvent(trigger, EventTriggerType.Drag, UpdatePassDrag);
        AddPassEvent(trigger, EventTriggerType.PointerUp, EndPassDrag);
        AddPassEvent(trigger, EventTriggerType.InitializePotentialDrag,
            data => data.useDragThreshold = false);
    }

    private static void AddPassEvent(EventTrigger trigger, EventTriggerType type,
        UnityAction<PointerEventData> callback)
    {
        EventTrigger.Entry entry = new() { eventID = type };
        entry.callback.AddListener(data => callback((PointerEventData)data));
        trigger.triggers.Add(entry);
    }

    private bool IsSwitching => entryInputLocked || portraitSlide != null || passReleasePlaying || likeAnimationPlaying;

    private void ConfigureLikeButton()
    {
        HideLikeFeedback();
        if (likeButton == null) return;
        EventTrigger trigger = likeButton.GetComponent<EventTrigger>();
        if (trigger == null) trigger = likeButton.gameObject.AddComponent<EventTrigger>();
        AddPassEvent(trigger, EventTriggerType.PointerDown, BeginLikePress);
    }

    private void BeginLikePress(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || holdingPass ||
            IsSwitching || character == null || !likeButton.IsInteractable()) return;
        Match();
    }

    private void BeginPassDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || holdingPass ||
            IsSwitching || characterOrder.Count == 0 || !passButton.IsInteractable()) return;
        holdingPass = true;
        passPointerId = data.pointerId;
        passPressPosition = data.position;
        passDragDirection = 0;
        likeWasInteractable = likeButton != null && likeButton.interactable;
        if (likeButton != null) likeButton.interactable = false;
        HidePassFeedback();
        SetHintObject(passPressedObject, true);
    }

    private void UpdatePassDrag(PointerEventData data)
    {
        if (!holdingPass || data.pointerId != passPointerId) return;
        float displacement = data.position.x - passPressPosition.x;
        int direction = Mathf.Abs(displacement) >= passDragThreshold ? (displacement > 0f ? 1 : -1) : 0;
        SetPassDragDirection(direction);
    }

    private void SetPassDragDirection(int direction)
    {
        if (passDragDirection == direction) return;
        passDragDirection = direction;
        onDragHintCleared.Invoke();
        SetDragHintVisuals(direction, direction == 0 ? DragHintState.Idle : DragHintState.Held);
        if (direction < 0) onLeftDragHint.Invoke();
        else if (direction > 0) onRightDragHint.Invoke();
    }

    private void EndPassDrag(PointerEventData data)
    {
        if (!holdingPass || data.pointerId != passPointerId ||
            data.button != PointerEventData.InputButton.Left) return;
        UpdatePassDrag(data);
        FinishPassDrag();
    }

    private void FinishPassDrag()
    {
        if (!holdingPass) return;
        int direction = passDragDirection;
        CancelPassDrag(false);
        if (direction != 0 && StartPassSlide(direction)) return;
        SetDragHintVisuals(0, DragHintState.Idle);
        GameAudioManager.PlaySe(passCancelledSe, seVolume);
    }

    private void CancelPassDrag(bool restoreIdle = true)
    {
        if (!holdingPass) return;
        holdingPass = false;
        passDragDirection = 0;
        onDragHintCleared.Invoke();
        if (likeButton != null) likeButton.interactable = likeWasInteractable;
        SetHintObject(passPressedObject, false);
        if (restoreIdle) SetDragHintVisuals(0, DragHintState.Idle);
    }

    private IEnumerator PlayReleaseFeedback(GameObject releasedObject, string animationState, string label,
        bool hideAfterPlayback = true)
    {
        Animator animator = null;
        int playingState = 0;
        if (releasedObject != null)
        {
            SetHintObject(releasedObject, true);
            animator = releasedObject.GetComponentInChildren<Animator>(true);
            if (!releasedObject.activeInHierarchy || animator == null || !animator.isActiveAndEnabled ||
                animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning(label + " animation object needs an active Animator with a controller.", this);
                animator = null;
            }
            else if (!string.IsNullOrEmpty(animationState) &&
                !animator.HasState(0, Animator.StringToHash(animationState)))
            {
                Debug.LogWarning(label + " animation state was not found: " + animationState, this);
                animator = null;
            }
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            if (!string.IsNullOrEmpty(animationState))
                animator.Play(animationState, 0, 0f);
            else
                animator.Play(0, 0, 0f);
            animator.Update(0f);
            playingState = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        }

        // Yield even without an animation so the same PointerUp/Click cannot commit twice.
        yield return null;
        while (animator != null && animator.isActiveAndEnabled && releasedObject != null &&
            releasedObject.activeInHierarchy)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.length <= 0f || state.normalizedTime >= 1f || state.fullPathHash != playingState)
                break;
            yield return null;
        }

        if (hideAfterPlayback) SetHintObject(releasedObject, false);
    }

    private IEnumerator PlayPassReleaseFeedback()
    {
        yield return PlayReleaseFeedback(passReleasedObject, passReleaseAnimationState, "Pass");
        RestoreButtonGraphics(hiddenPassGraphics);
        passReleasePlaying = false;
        releaseObjectRoutine = null;
        RestoreButtonInteraction();
    }

    private IEnumerator PlayLikeAnimationAndMatch(CharacterData selectedCharacter)
    {
        yield return PlayReleaseFeedback(likePressedObject, likeAnimationState, "Like", false);
        yield return new WaitForSecondsRealtime(likeSceneDelay);
        // Keep Like hidden and locked during the animation and delay.
        GameSession.SelectCharacter(selectedCharacter);
        SceneManager.LoadScene("ChatScene");
    }

    private void HideButtonGraphics(Button button, Dictionary<Graphic, bool> hiddenGraphics)
    {
        if (button == null) return;
        foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
        {
            // A/B may be children of the button; their animations remain visible.
            if (IsFeedbackGraphic(graphic.transform)) continue;
            hiddenGraphics[graphic] = graphic.enabled;
            graphic.enabled = false;
        }
    }

    private bool IsFeedbackGraphic(Transform graphic)
    {
        return (passPressedObject != null && graphic.IsChildOf(passPressedObject.transform)) ||
            (passReleasedObject != null && graphic.IsChildOf(passReleasedObject.transform)) ||
            (likePressedObject != null && graphic.IsChildOf(likePressedObject.transform));
    }

    private static void RestoreButtonGraphics(Dictionary<Graphic, bool> hiddenGraphics)
    {
        foreach (KeyValuePair<Graphic, bool> item in hiddenGraphics)
            if (item.Key != null) item.Key.enabled = item.Value;
        hiddenGraphics.Clear();
    }

    private void LockButtonInteraction()
    {
        passWasInteractable = passButton != null && passButton.interactable;
        likeWasInteractable = likeButton != null && likeButton.interactable;
        if (passButton != null) passButton.interactable = false;
        if (likeButton != null) likeButton.interactable = false;
    }

    private void RestoreButtonInteraction()
    {
        if (IsSwitching || holdingPass) return;
        if (passButton != null) passButton.interactable = passWasInteractable;
        if (likeButton != null) likeButton.interactable = likeWasInteractable;
    }

    private void HidePassFeedback()
    {
        if (releaseObjectRoutine != null)
        {
            StopCoroutine(releaseObjectRoutine);
            releaseObjectRoutine = null;
        }
        SetHintObject(passPressedObject, false);
        SetHintObject(passReleasedObject, false);
    }

    private void HideLikeFeedback()
    {
        if (likeAnimationRoutine != null)
        {
            StopCoroutine(likeAnimationRoutine);
            likeAnimationRoutine = null;
        }
        SetHintObject(likePressedObject, false);
    }

    private void SetDragHintVisuals(int direction, DragHintState state)
    {
        SetDragHintVisual(leftDragHint, leftDragHintStates, direction < 0 ? state : DragHintState.Idle);
        SetDragHintVisual(rightDragHint, rightDragHintStates, direction > 0 ? state : DragHintState.Idle);
    }

    private void SetDragHintVisual(GameObject root, DragHintVisuals visuals, DragHintState state)
    {
        if (!visuals.IsConfigured)
        {
            // Preserve the original hint behavior until state objects are assigned.
            SetHintObject(root, state != DragHintState.Idle);
            return;
        }

        SetHintObject(root, true);
        SetHintObject(visuals.idleImage, state == DragHintState.Idle);
        SetHintObject(visuals.heldAnimation, state == DragHintState.Held);
        SetHintObject(visuals.releasedImage, state == DragHintState.Released);
    }

    private void SetHintObject(GameObject visual, bool visible)
    {
        if (visual == null) return;
        // Deactivating a button/controller ancestor would interrupt the gesture.
        if (transform.IsChildOf(visual.transform) ||
            (passButton != null && passButton.transform.IsChildOf(visual.transform)) ||
            (likeButton != null && likeButton.transform.IsChildOf(visual.transform)))
        {
            Debug.LogError("Feedback objects must not be the buttons themselves or ancestors of the buttons/controller.", this);
            return;
        }
        foreach (Graphic graphic in visual.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        visual.SetActive(visible);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CancelPassDrag();
        }
    }

    private bool StartPassSlide(int direction)
    {
        if (characterOrder.Count == 0 || IsSwitching) return false;
        int nextIndex = (characterIndex + 1) % characterOrder.Count;
        if (nextIndex == characterIndex)
        {
            SetDragHintVisuals(0, DragHintState.Idle);
            return false;
        }

        LockButtonInteraction();
        GameAudioManager.PlaySe(passChangedSe, seVolume);
        passReleasePlaying = true;
        HideButtonGraphics(passButton, hiddenPassGraphics);
        releaseObjectRoutine = StartCoroutine(PlayPassReleaseFeedback());
        if (portraitImage == null || portraitSlideDuration <= 0f)
        {
            ShowCharacter(nextIndex);
            SetDragHintVisuals(0, DragHintState.Idle);
            return true;
        }

        portraitRestPosition = portraitImage.rectTransform.anchoredPosition;
        SetDragHintVisuals(direction, DragHintState.Released);
        if (direction < 0) onLeftDragReleased.Invoke();
        else onRightDragReleased.Invoke();
        portraitSlide = StartCoroutine(SlidePortrait(nextIndex, direction));
        return true;
    }

    private void ShowCharacter(int index)
    {
        characterIndex = index;
        character = characterOrder[characterIndex];
        RefreshCharacter();
    }

    private void ConfigurePortraitViewport()
    {
        if (portraitImage == null) return;
        RectTransform portraitRect = portraitImage.rectTransform;
        if (portraitViewport == null)
        {
            // Older scenes get a clipping rectangle matching the original portrait.
            portraitViewport = new GameObject("PortraitViewport", typeof(RectTransform), typeof(RectMask2D))
                .GetComponent<RectTransform>();
            portraitViewport.SetParent(portraitRect.parent, false);
            portraitViewport.SetSiblingIndex(portraitRect.GetSiblingIndex());
            portraitViewport.anchorMin = portraitRect.anchorMin;
            portraitViewport.anchorMax = portraitRect.anchorMax;
            portraitViewport.pivot = portraitRect.pivot;
            portraitViewport.sizeDelta = portraitRect.sizeDelta;
            portraitViewport.anchoredPosition3D = portraitRect.anchoredPosition3D;
            portraitViewport.localRotation = portraitRect.localRotation;
            portraitViewport.localScale = portraitRect.localScale;
        }

        if (portraitViewport == portraitRect || portraitViewport.IsChildOf(portraitRect))
        {
            Debug.LogError("Portrait Viewport must be a separate rectangle outside the Portrait hierarchy.", this);
            portraitViewport = null;
            return;
        }

        if (portraitViewport.GetComponent<RectMask2D>() == null)
            portraitViewport.gameObject.AddComponent<RectMask2D>();
        if (portraitRect.parent != portraitViewport)
            portraitRect.SetParent(portraitViewport, true);
        portraitImage.maskable = true;
    }

    private IEnumerator SlidePortrait(int nextIndex, int direction)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform portraitRect = portraitImage.rectTransform;
        Transform parent = portraitRect.parent;
        Bounds portraitBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, portraitRect);
        // Read the viewport's own rectangle, not bounds including moving children.
        Rect visibleRect = ((RectTransform)parent).rect;
        float distance = Mathf.Max(visibleRect.xMax - portraitBounds.min.x,
            portraitBounds.max.x - visibleRect.xMin) + 1f;

        incomingPortrait = Instantiate(portraitImage, parent);
        incomingPortrait.name = "IncomingPortrait";
        incomingPortrait.transform.SetSiblingIndex(portraitRect.GetSiblingIndex() + 1);
        incomingPortrait.sprite = characterOrder[nextIndex].ProfileImage;
        incomingPortrait.raycastTarget = false;
        Vector2 travel = Vector2.right * (distance * direction);
        incomingPortrait.rectTransform.anchoredPosition = portraitRestPosition - travel;

        float elapsed = 0f;
        while (elapsed < portraitSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / portraitSlideDuration));
            portraitRect.anchoredPosition = portraitRestPosition + travel * progress;
            incomingPortrait.rectTransform.anchoredPosition = portraitRestPosition - travel * (1f - progress);
            yield return null;
        }

        ShowCharacter(nextIndex);
        portraitSlide = null;
        RestorePortrait();
    }

    private void RestorePortrait()
    {
        if (portraitImage != null)
            portraitImage.rectTransform.anchoredPosition = portraitRestPosition;
        if (incomingPortrait != null)
        {
            incomingPortrait.gameObject.SetActive(false);
            Destroy(incomingPortrait.gameObject);
            incomingPortrait = null;
        }
        RestoreButtonInteraction();
        SetDragHintVisuals(0, DragHintState.Idle);
    }

    private void OnDisable()
    {
        CancelPassDrag();
        HidePassFeedback();
        HideLikeFeedback();
        bool wasSwitching = IsSwitching;
        if (entryInputRoutine != null)
        {
            StopCoroutine(entryInputRoutine);
            entryInputRoutine = null;
        }
        entryInputLocked = false;
        passReleasePlaying = false;
        likeAnimationPlaying = false;
        RestoreButtonGraphics(hiddenPassGraphics);
        RestoreButtonGraphics(hiddenLikeGraphics);
        if (portraitSlide != null)
        {
            StopCoroutine(portraitSlide);
            portraitSlide = null;
            RestorePortrait();
        }
        else if (wasSwitching)
            RestoreButtonInteraction();
    }

    public void Match()
    {
        if (character == null || IsSwitching || holdingPass) return;
        LockButtonInteraction();
        likeAnimationPlaying = true;
        GameAudioManager.PlaySe(likeSe, seVolume);
        HideButtonGraphics(likeButton, hiddenLikeGraphics);
        likeAnimationRoutine = StartCoroutine(PlayLikeAnimationAndMatch(character));
    }
}
