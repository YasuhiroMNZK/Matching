public static class GameSession
{
    public static CharacterData Character { get; private set; }
    public static EndingData Ending { get; private set; }
    public static EndingType EndingType { get; private set; }
    public static int FinalHonesty { get; private set; }
    public static int AcquiredWordCount { get; private set; }

    public static void SelectCharacter(CharacterData character)
    {
        Character = character;
        Ending = null;
        FinalHonesty = 0;
        AcquiredWordCount = 0;
    }

    public static void Complete(EndingData ending, EndingType type, int honesty, int wordCount)
    {
        Ending = ending;
        EndingType = type;
        FinalHonesty = honesty;
        AcquiredWordCount = wordCount;
    }

    public static void Reset()
    {
        Character = null;
        Ending = null;
        FinalHonesty = 0;
        AcquiredWordCount = 0;
    }
}
