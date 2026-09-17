using UnityEngine;
using UnityEngine.Events;
[System.Serializable]
public class ChatData
{
    [Header("深堀")]
    [TextArea(2, 5)]
    public string rightPlayer;

    [TextArea(2, 5)]
    public string rightNpc;

    [Header("相槌")]
    [TextArea(2, 5)]
    public string wrongPlayer;

    [TextArea(2, 5)]
    public string wrongNpc;
}

public class DialogueManager : MonoBehaviour
{
    [Header("New chat flow")]
    [SerializeField]
    private ChatManager flowManager;

    [Header("chatManager")]
    public ChattingManager chatManager;

    [Header("chatData")]
    public ChatData[] dialogues;

    [Header("Events")]
    public UnityEvent[] rightEvents;
    private int progress = 0;
    private int scrollIndex = 0;

    public void WrongChoice()
    {
        if (flowManager != null)
        {
            flowManager.ChooseAizuchi();
            return;
        }

        if (progress >= dialogues.Length)
            return;

        chatManager.AddChat(
            dialogues[progress].wrongPlayer,
            dialogues[progress].wrongNpc
        );
        chatManager.ScrollToStep(scrollIndex);
        scrollIndex++;
    }

   
public void RightChoice()
{
    if (flowManager != null)
    {
        flowManager.ChooseDeepDive();
        return;
    }

    if (progress >= dialogues.Length)
        return;

    chatManager.AddChat(
        dialogues[progress].rightPlayer,
        dialogues[progress].rightNpc
    );
    chatManager.ScrollToStep(scrollIndex);
    scrollIndex++;
    
    if (progress < rightEvents.Length)
        {
            rightEvents[progress].Invoke();
        }

    progress++;
}
    }
