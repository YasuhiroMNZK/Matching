using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class IntValueEvent : UnityEvent<int>
{
}

public sealed class HonestyManager : MonoBehaviour
{
    public const int AizuchiChange = 20;
    public const int DeepDiveChange = -15;
    public const int WeaknessRequirement = 70;
    public const int CoreRequirement = 90;

    [SerializeField]
    private IntValueEvent onHonestyChanged = new();

    public int Current { get; private set; }
    public bool CanAccessWeakness => Current >= WeaknessRequirement;
    public IntValueEvent OnHonestyChanged => onHonestyChanged;

    public void Initialize(CharacterData character)
    {
        if (character == null)
            throw new ArgumentNullException(nameof(character));

        Set(character.InitialHonesty);
    }

    public bool IsTruthful(TopicData topic)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        return topic.Genre == TopicGenre.Weakness || Current >= topic.HonestyThreshold;
    }

    public void ApplyAizuchi()
    {
        Change(AizuchiChange);
    }

    public void ApplyDeepDive()
    {
        Change(DeepDiveChange);
    }

    public void Set(int value)
    {
        Current = Mathf.Clamp(value, 0, 100);
        onHonestyChanged.Invoke(Current);
    }

    private void Change(int amount)
    {
        Set(Current + amount);
    }
}
