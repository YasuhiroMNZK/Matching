using UnityEngine;

public enum WordGenre
{
    Place,
    Action,
    Purpose,
    Weakness
}

[CreateAssetMenu(
    fileName = "WordData",
    menuName = "JingaiMatch/Word Data"
)]
public class WordData : ScriptableObject
{
    [Header("Basic Info")]

    [SerializeField]
    private string wordId;

    [SerializeField]
    private string displayText;

    [SerializeField]
    private WordGenre genre;


    public string WordId => wordId;

    public string DisplayText => displayText;

    public WordGenre Genre => genre;
}