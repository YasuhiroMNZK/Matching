using System;

[Serializable]
public sealed class AcquiredWord
{
    public AcquiredWord(WordData data, TopicData sourceTopic, bool isTruth)
    {
        Data = data;
        SourceTopic = sourceTopic;
        IsTruth = isTruth;
    }

    public WordData Data { get; }
    public TopicData SourceTopic { get; }
    public bool IsTruth { get; }
}
