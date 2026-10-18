using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EndingData",
    menuName = "JingaiMatch/Ending Data"
)]
public class EndingData : ScriptableObject
{
    [Header("Basic Info")]

    [SerializeField]
    private string endingId;

    [SerializeField]
    private EndingType endingType;

    [SerializeField]
    private string endingTitle;

    [Header("Background")]
    [SerializeField] private Sprite backgroundImage;


    [Header("Ending Text")]

    [SerializeField]
    [TextArea(5, 15)]
    private string endingText;

    [SerializeField, TextArea(2, 6)]
    [Tooltip("Displayed one entry per click. When empty, Ending Text is displayed one non-empty line per click.")]
    private List<string> endingLines = new();


    [Header("Rewards")]

    [SerializeField]
    private List<TagData> unlockTags = new();


    public string EndingId => endingId;

    public EndingType EndingType => endingType;

    public string EndingTitle => endingTitle;

    public Sprite BackgroundImage => backgroundImage;

    public string EndingText => endingText;

    public IReadOnlyList<string> EndingLines => endingLines;

    public IReadOnlyList<TagData> UnlockTags => unlockTags;
}
