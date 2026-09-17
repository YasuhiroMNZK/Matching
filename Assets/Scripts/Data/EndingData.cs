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


    [Header("Ending Text")]

    [SerializeField]
    [TextArea(5, 15)]
    private string endingText;


    [Header("Rewards")]

    [SerializeField]
    private List<TagData> unlockTags = new();


    public string EndingId => endingId;

    public EndingType EndingType => endingType;

    public string EndingTitle => endingTitle;

    public string EndingText => endingText;

    public IReadOnlyList<TagData> UnlockTags => unlockTags;
}