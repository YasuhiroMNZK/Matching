using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Tag slots")]
    [SerializeField] private GameObject[] tagSlots = new GameObject[4];

    private void Start()
    {
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

        SceneUiFactory.ApplyJapaneseFont(uiRoot);
        RefreshCharacter();
        if (likeButton == null) Debug.LogError("MatchingScene Like Button is not assigned.", this);
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
        if (characterOrder.Count == 0) return;
        characterIndex = (characterIndex + 1) % characterOrder.Count;
        character = characterOrder[characterIndex];
        RefreshCharacter();
    }

    public void Match()
    {
        if (character == null) return;
        GameSession.SelectCharacter(character);
        SceneManager.LoadScene("ChatScene");
    }
}
