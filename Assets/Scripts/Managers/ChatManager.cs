using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class TopicEvent : UnityEvent<TopicData>
{
}

[Serializable]
public sealed class ChatReplyEvent : UnityEvent<string, string>
{
}

public enum ChatState
{
    NotStarted,
    AwaitingCommand,
    DateInvitation,
    Finished
}

public sealed class ChatManager : MonoBehaviour
{
    public const int MaximumTurns = 10;

    [Header("Session")]
    [SerializeField]
    private CharacterData character;

    [SerializeField]
    private bool startOnStart;

    [Header("Managers")]
    [SerializeField]
    private HonestyManager honestyManager;

    [SerializeField]
    private WordManager wordManager;

    [Header("Optional legacy UI")]
    [SerializeField]
    private ChattingManager chattingView;

    [SerializeField]
    private Color wordHighlightColor = new(0f, 0.55f, 0.85f, 1f);

    [Header("Events")]
    [SerializeField]
    private TopicEvent onTopicStarted = new();

    [SerializeField]
    private ChatReplyEvent onReply = new();

    [SerializeField]
    private WordAcquiredEvent onWordAcquired = new();

    [SerializeField]
    private UnityEvent onDateInvitationRequired = new();

    private readonly Dictionary<TopicGenre, List<TopicData>> remainingTopics = new();
    private TopicGenre? currentGenre;

    public CharacterData Character => character;
    public HonestyManager Honesty => honestyManager;
    public WordManager Words => wordManager;
    public TopicData CurrentTopic { get; private set; }
    public int CompletedTurns { get; private set; }
    public ChatState State { get; private set; } = ChatState.NotStarted;
    public bool CanInviteToDate => wordManager != null && wordManager.HasInvitationWords;
    public bool CanChooseWeakness => honestyManager != null && honestyManager.CanAccessWeakness && HasRemaining(TopicGenre.Weakness);
    public TopicEvent OnTopicStarted => onTopicStarted;
    public ChatReplyEvent OnReply => onReply;
    public WordAcquiredEvent OnWordAcquired => onWordAcquired;
    public UnityEvent OnDateInvitationRequired => onDateInvitationRequired;

    public void Configure(CharacterData selectedCharacter, HonestyManager honesty, WordManager words)
    {
        character = selectedCharacter;
        honestyManager = honesty;
        wordManager = words;
    }

    private void Start()
    {
        if (startOnStart && character != null)
            StartChat(character);
    }

    public void StartChat(CharacterData selectedCharacter)
    {
        if (selectedCharacter == null)
            throw new ArgumentNullException(nameof(selectedCharacter));
        if (honestyManager == null)
            throw new InvalidOperationException("HonestyManager is not assigned.");
        if (wordManager == null)
            throw new InvalidOperationException("WordManager is not assigned.");

        character = selectedCharacter;
        honestyManager.Initialize(character);
        wordManager.ResetSession();
        BuildTopicPools();
        CurrentTopic = null;
        currentGenre = null;
        CompletedTurns = 0;
        State = ChatState.AwaitingCommand;

        if (!string.IsNullOrWhiteSpace(character.FirstMessage))
            PublishMessage(string.Empty, character.FirstMessage);
    }

    public bool BeginNextTopic()
    {
        return BeginNextTopic(null);
    }

    public bool BeginNextTopic(TopicGenre genre)
    {
        if (genre == TopicGenre.Weakness && !CanChooseWeakness)
            return false;

        return BeginNextTopic((TopicGenre?)genre);
    }

    public bool ChooseAizuchi()
    {
        if (!TryPrepareTopic(false))
            return false;

        TopicData topic = CurrentTopic;
        PublishTopicDialogue(topic, topic.AizuchiPlayerText, topic.AizuchiReplyText);
        honestyManager.ApplyAizuchi();
        CompleteTurn();
        return true;
    }

    public bool ChooseDeepDive()
    {
        if (!TryPrepareTopic(false))
            return false;

        TopicData topic = CurrentTopic;
        bool isTruth = honestyManager.IsTruthful(topic);
        string reply = isTruth ? topic.DeepDiveTrueReplyText : topic.DeepDiveFalseReplyText;

        AcquiredWord acquired = wordManager.AcquireFrom(topic, isTruth);
        if (acquired != null)
        {
            reply = HighlightWord(reply, acquired.Data.DisplayText);
            onWordAcquired.Invoke(acquired.Data);
        }

        PublishTopicDialogue(topic, topic.DeepDivePlayerText, reply);

        honestyManager.ApplyDeepDive();
        CompleteTurn();
        return true;
    }

    public bool ChooseSmallTalk(TopicGenre nextGenre)
    {
        if (!TryPrepareTopic(nextGenre))
            return false;

        TopicData topic = CurrentTopic;
        PublishTopicDialogue(topic, topic.SmallTalkPlayerText, topic.SmallTalkReplyText);
        CompleteTurn();
        return true;
    }

    public bool ChooseSmallTalk()
    {
        if (!TryPrepareTopic(true))
            return false;

        TopicData topic = CurrentTopic;
        PublishTopicDialogue(topic, topic.SmallTalkPlayerText, topic.SmallTalkReplyText);
        CompleteTurn();
        return true;
    }

    public bool InviteToDate()
    {
        if (State != ChatState.AwaitingCommand || !CanInviteToDate)
            return false;

        CurrentTopic = null;
        State = ChatState.DateInvitation;
        onDateInvitationRequired.Invoke();
        return true;
    }

    public EndingData CompleteInvitation()
    {
        if (State != ChatState.DateInvitation)
            throw new InvalidOperationException("The chat is not in the date invitation phase.");

        EndingType result = wordManager.EvaluateInvitation();
        State = ChatState.Finished;

        return result switch
        {
            EndingType.Bad => character.BadEnding,
            EndingType.Good => character.GoodEnding,
            EndingType.Perfect => character.PerfectEnding,
            _ => null
        };
    }

    private bool BeginNextTopic(TopicGenre? requestedGenre)
    {
        if (State == ChatState.DateInvitation || State == ChatState.Finished)
            return false;

        List<TopicGenre> availableGenres = GetAvailableGenres();
        if (requestedGenre.HasValue)
        {
            if (!HasRemaining(requestedGenre.Value))
                return false;
            if (requestedGenre.Value == TopicGenre.Weakness && !CanChooseWeakness)
                return false;
        }

        if (!requestedGenre.HasValue && availableGenres.Count == 0)
        {
            RequireDateInvitation();
            return false;
        }

        TopicGenre genre = requestedGenre ?? availableGenres[UnityEngine.Random.Range(0, availableGenres.Count)];
        List<TopicData> pool = remainingTopics[genre];
        if (genre == TopicGenre.Weakness)
        {
            List<TopicData> eligible = pool.FindAll(topic => honestyManager.Current >= topic.HonestyThreshold);
            if (eligible.Count == 0)
                return false;
            CurrentTopic = eligible[UnityEngine.Random.Range(0, eligible.Count)];
            pool.Remove(CurrentTopic);
        }
        else
        {
            CurrentTopic = pool[0];
            pool.RemoveAt(0);
        }
        currentGenre = genre;
        State = ChatState.AwaitingCommand;
        onTopicStarted.Invoke(CurrentTopic);

        return true;
    }

    private void CompleteTurn()
    {
        CompletedTurns++;
        CurrentTopic = null;
        if (CompletedTurns >= MaximumTurns || GetAvailableGenres().Count == 0)
            RequireDateInvitation();
    }

    private void RequireDateInvitation()
    {
        CurrentTopic = null;
        State = ChatState.DateInvitation;
        onDateInvitationRequired.Invoke();
    }

    private void PublishTopicDialogue(TopicData topic, string playerText, string replyText)
    {
        PublishMessage(playerText, topic.IntroText);
        PublishMessage(string.Empty, replyText);
    }

    private void PublishMessage(string playerText, string npcText)
    {
        onReply.Invoke(playerText, npcText);
        if (chattingView != null)
            chattingView.AddChat(playerText, npcText);
    }

    private string HighlightWord(string source, string word)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(word))
            return source;

        int firstMatch = source.IndexOf(word, StringComparison.Ordinal);
        if (firstMatch < 0)
            return source;

        string color = ColorUtility.ToHtmlStringRGB(wordHighlightColor);
        string openingTag = $"<color=#{color}>";
        const string closingTag = "</color>";
        StringBuilder result = new(source.Length + openingTag.Length + closingTag.Length);
        int copyFrom = 0;
        int match = firstMatch;

        while (match >= 0)
        {
            result.Append(source, copyFrom, match - copyFrom);
            result.Append(openingTag);
            result.Append(word);
            result.Append(closingTag);
            copyFrom = match + word.Length;
            match = source.IndexOf(word, copyFrom, StringComparison.Ordinal);
        }

        result.Append(source, copyFrom, source.Length - copyFrom);
        return result.ToString();
    }

    private bool CanExecuteCommand()
    {
        return State == ChatState.AwaitingCommand;
    }

    private bool TryPrepareTopic(bool switchGenre)
    {
        if (!CanExecuteCommand())
            return false;
        if (CurrentTopic != null)
            return true;

        List<TopicGenre> candidates = GetAvailableGenres();
        if (switchGenre && currentGenre.HasValue && candidates.Count > 1)
            candidates.Remove(currentGenre.Value);

        if (candidates.Count == 0)
        {
            RequireDateInvitation();
            return false;
        }

        TopicGenre selectedGenre;
        if (!switchGenre && currentGenre.HasValue && candidates.Contains(currentGenre.Value))
            selectedGenre = currentGenre.Value;
        else
            selectedGenre = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        return BeginNextTopic(selectedGenre);
    }

    private bool TryPrepareTopic(TopicGenre genre)
    {
        if (!CanExecuteCommand() || !HasRemaining(genre))
            return false;
        if (genre == TopicGenre.Weakness && !CanChooseWeakness)
            return false;
        return BeginNextTopic(genre);
    }

    private void BuildTopicPools()
    {
        remainingTopics.Clear();
        foreach (TopicGenre genre in Enum.GetValues(typeof(TopicGenre)))
        {
            List<TopicData> pool = new();
            IReadOnlyList<TopicData> source = character.GetTopics(genre);
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    pool.Add(source[i]);
            }
            remainingTopics.Add(genre, pool);
        }
    }

    private List<TopicGenre> GetAvailableGenres()
    {
        List<TopicGenre> genres = new();
        foreach (KeyValuePair<TopicGenre, List<TopicData>> pair in remainingTopics)
        {
            if (pair.Value.Count == 0 || pair.Key == TopicGenre.Weakness)
                continue;
            genres.Add(pair.Key);
        }
        return genres;
    }

    private bool HasRemaining(TopicGenre genre)
    {
        if (!remainingTopics.TryGetValue(genre, out List<TopicData> topics))
            return false;

        if (genre != TopicGenre.Weakness)
            return topics.Count > 0;

        return topics.Exists(topic => honestyManager != null && honestyManager.Current >= topic.HonestyThreshold);
    }
}
