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

    private void RefreshTags()
    {
        // Keep the scene layout editable. Missing Inspector references are resolved
        // from the existing Tag1-Tag4 objects, including inactive objects.

        List<TagData> candidates = new();
        HashSet<string> seenNames = new();
        foreach (TagData tag in character.Tags)
        {
            if (tag != null && !string.IsNullOrWhiteSpace(tag.DisplayName) && seenNames.Add(tag.DisplayName))
                candidates.Add(tag);
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
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

    public void Pass() {
        
    }

    public void Match()
    {
        GameSession.SelectCharacter(character);
        SceneManager.LoadScene("ChatScene");
    }
}
