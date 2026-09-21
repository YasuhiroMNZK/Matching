using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class WordAcquiredEvent : UnityEvent<WordData>
{
}

public sealed class WordManager : MonoBehaviour
{
    [SerializeField]
    private WordAcquiredEvent onWordAcquired = new();

    [SerializeField]
    private UnityEvent onInvitationWordsReady = new();

    private readonly List<AcquiredWord> acquiredWords = new();

    public IReadOnlyList<AcquiredWord> AcquiredWords => acquiredWords;
    public WordData SelectedPlace { get; private set; }
    public WordData SelectedAction { get; private set; }
    public WordData SelectedPurpose { get; private set; }
    public bool HasWeakness => acquiredWords.Exists(word => word.Data.Genre == WordGenre.Weakness);

    public bool HasInvitationWords =>
        HasGenre(WordGenre.Place) &&
        HasGenre(WordGenre.Action) &&
        HasGenre(WordGenre.Purpose);

    public bool IsInvitationComplete =>
        SelectedPlace != null &&
        SelectedAction != null &&
        SelectedPurpose != null;

    public WordAcquiredEvent OnWordAcquired => onWordAcquired;
    public UnityEvent OnInvitationWordsReady => onInvitationWordsReady;

    public void ResetSession()
    {
        acquiredWords.Clear();
        ClearSelection();
    }

    public AcquiredWord AcquireFrom(TopicData topic, bool isTruth)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        if (topic.Genre == TopicGenre.Weakness)
            isTruth = true;

        IReadOnlyList<WordData> candidates = topic.GetWords(isTruth);
        List<WordData> available = new();

        for (int i = 0; i < candidates.Count; i++)
        {
            WordData candidate = candidates[i];
            if (candidate != null && !IsAcquired(candidate))
                available.Add(candidate);
        }

        if (available.Count == 0)
            return null;

        WordData selected = available[UnityEngine.Random.Range(0, available.Count)];
        AcquiredWord acquired = new(selected, topic, isTruth);
        bool wasReady = HasInvitationWords;

        acquiredWords.Add(acquired);
        onWordAcquired.Invoke(selected);

        if (!wasReady && HasInvitationWords)
            onInvitationWordsReady.Invoke();

        return acquired;
    }

    public bool TrySelect(WordData word)
    {
        if (word == null || !IsAcquired(word))
            return false;

        switch (word.Genre)
        {
            case WordGenre.Place:
                SelectedPlace = word;
                return true;
            case WordGenre.Action:
                SelectedAction = word;
                return true;
            case WordGenre.Purpose:
                SelectedPurpose = word;
                return true;
            default:
                return false;
        }
    }

    public EndingType EvaluateInvitation(bool hasTriggeredWeakness)
    {
        if (!IsInvitationComplete)
            throw new InvalidOperationException("All three invitation slots must be filled before evaluation.");

        if (!IsTruth(SelectedPlace) || !IsTruth(SelectedAction) || !IsTruth(SelectedPurpose))
            return EndingType.Bad;

        return hasTriggeredWeakness ? EndingType.Perfect : EndingType.Good;
    }

    public void ClearSelection()
    {
        SelectedPlace = null;
        SelectedAction = null;
        SelectedPurpose = null;
    }

    public bool IsAcquired(WordData word)
    {
        return acquiredWords.Exists(acquired => acquired.Data == word);
    }

    public bool IsTruth(WordData word)
    {
        AcquiredWord acquired = acquiredWords.Find(item => item.Data == word);
        return acquired != null && acquired.IsTruth;
    }

    private bool HasGenre(WordGenre genre)
    {
        return acquiredWords.Exists(word => word.Data.Genre == genre);
    }
}
