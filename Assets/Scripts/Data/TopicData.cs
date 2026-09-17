using System.Collections.Generic;
using UnityEngine;

public enum TopicGenre
{
Place,
Action,
Purpose,
Weakness
}

[CreateAssetMenu(
    fileName = "TopicData",
    menuName = "JingaiMatch/Topic Data"
)]
public class TopicData : ScriptableObject
{
    [Header("Basic Info")]

    [SerializeField]
    private string topicId;

    [SerializeField]
    private TopicGenre genre;


    [Header("Honesty")]

    [SerializeField]
    [Range(0, 100)]
    private int honestyThreshold = 50;

[Header("Dialogue - Intro")]

[SerializeField]
[TextArea(2, 5)]
private string introText;


[Header("Dialogue - Aizuchi")]

[SerializeField]
[TextArea(2, 5)]
private string aizuchiPlayerText;

[SerializeField]
[TextArea(2, 5)]
private string aizuchiReplyText;


[Header("Dialogue - Deep Dive")]

[SerializeField]
[TextArea(2, 5)]
private string deepDivePlayerText;

[SerializeField]
[TextArea(2, 5)]
private string deepDiveTrueReplyText;

[SerializeField]
[TextArea(2, 5)]
private string deepDiveFalseReplyText;

[Header("Dialogue - Small Talk")]

[SerializeField]
[TextArea(2, 5)]
private string smallTalkPlayerText;

[SerializeField]
[TextArea(2, 5)]
private string smallTalkReplyText;


    [Header("Words")]

    [SerializeField]
    private List<WordData> trueWords = new();

    [SerializeField]
    private List<WordData> falseWords = new();


    public string TopicId => topicId;

    public TopicGenre Genre => genre;

    public int HonestyThreshold => honestyThreshold;

    public string IntroText => introText;

    public string AizuchiReplyText => aizuchiReplyText;

    public string DeepDiveTrueReplyText => deepDiveTrueReplyText;

    public string DeepDiveFalseReplyText => deepDiveFalseReplyText;

    public string AizuchiPlayerText => aizuchiPlayerText;

    public string DeepDivePlayerText => deepDivePlayerText;

    public string SmallTalkPlayerText => smallTalkPlayerText;

    public string SmallTalkReplyText => smallTalkReplyText;

    public IReadOnlyList<WordData> TrueWords => trueWords;

    public IReadOnlyList<WordData> FalseWords => falseWords;

    public IReadOnlyList<WordData> GetWords(bool isTruth)
    {
        return isTruth ? trueWords : falseWords;
    }
}
