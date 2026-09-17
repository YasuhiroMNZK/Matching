using UnityEngine;

[CreateAssetMenu(
    fileName = "TagData",
    menuName = "JingaiMatch/Tag Data"
)]
public class TagData : ScriptableObject
{
    [Header("Basic Info")]

    [SerializeField]
    private string tagId;

    [SerializeField]
    private string displayName;

    [Header("Unlock")]

    [SerializeField]
    private bool unlockedByDefault = false;


    public string TagId => tagId;

    public string DisplayName => displayName;

    public bool UnlockedByDefault => unlockedByDefault;
}