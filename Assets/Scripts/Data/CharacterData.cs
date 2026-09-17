using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterData",
    menuName = "JingaiMatch/Character Data"
)]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]

    [SerializeField]
    private string characterId;

    [SerializeField]
    private string characterName;

    [SerializeField]
    private string species;

    [SerializeField]
    private string age;

    [SerializeField]
    private string gender;


    [Header("Profile")]

    [SerializeField]
    private Sprite profileImage;

    [SerializeField]
    private Sprite iconImage;

    [SerializeField]
    [TextArea(2, 5)]
    private string profileText;

    [SerializeField]
    private List<TagData> tags = new();


    [Header("Conversation Design")]

    [SerializeField]
    [TextArea(2, 5)]
    private string firstMessage;


    [Header("Chat")]

[SerializeField]
[Range(0, 100)]
private int initialHonesty = 50;

[SerializeField]
private List<TopicData> placeTopics = new();

[SerializeField]
private List<TopicData> actionTopics = new();

[SerializeField]
private List<TopicData> purposeTopics = new();

[SerializeField]
private List<TopicData> weaknessTopics = new();

[Header("Endings")]

[SerializeField]
[TextArea(2, 5)]
private string badText;

[SerializeField]
[TextArea(2, 5)]
private string goodText;

[SerializeField]
[TextArea(2, 5)]
private string perfectText;

[SerializeField]
private EndingData badEnding;

[SerializeField]
private EndingData goodEnding;

[SerializeField]
private EndingData perfectEnding;

    public string CharacterId => characterId;
    public string CharacterName => characterName;
    public string Species => species;
    public string Age => age;
    public string Gender => gender;

    public Sprite ProfileImage => profileImage;
    public Sprite IconImage => iconImage;
    public string ProfileText => profileText;

    public IReadOnlyList<TagData> Tags => tags;

    public string FirstMessage => firstMessage;
    public int InitialHonesty => initialHonesty;

    public IReadOnlyList<TopicData> PlaceTopics => placeTopics;
    public IReadOnlyList<TopicData> ActionTopics => actionTopics;
    public IReadOnlyList<TopicData> PurposeTopics => purposeTopics;
    public IReadOnlyList<TopicData> WeaknessTopics => weaknessTopics;

    public EndingData BadEnding => badEnding;
    public EndingData GoodEnding => goodEnding;
    public EndingData PerfectEnding => perfectEnding;
    public string BadText => badText;
    public string GoodText => goodText;
    public string PerfectText => perfectText;

    public string GetEndingText(EndingType endingType)
    {
        return endingType switch
        {
            EndingType.Bad => badText,
            EndingType.Good => goodText,
            EndingType.Perfect => perfectText,
            _ => string.Empty
        };
    }

    public IReadOnlyList<TopicData> GetTopics(TopicGenre genre)
    {
        return genre switch
        {
            TopicGenre.Place => placeTopics,
            TopicGenre.Action => actionTopics,
            TopicGenre.Purpose => purposeTopics,
            TopicGenre.Weakness => weaknessTopics,
            _ => System.Array.Empty<TopicData>()
        };
    }

}
