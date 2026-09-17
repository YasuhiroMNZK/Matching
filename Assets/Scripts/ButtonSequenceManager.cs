using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonSequenceManager : MonoBehaviour
{
    [Header("buttons")]
    public Button[] buttons;

    [Header("correctSequence")]
    public int[] correctSequence = { 2, 5, 0 };

    public Text[] selectedTexts;

    [Header("onComplete")]
    public UnityEvent onComplete;

    [Header("onSuccess")]
    public UnityEvent onSuccess;

    [Header("onFail")]
    public UnityEvent onFail;

    private List<int> playerSequence = new List<int>();

    private void Start()
    {

        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => PressButton(index));
        }
    }
    void PressButton(int index)
    {
        if (playerSequence.Count >= correctSequence.Length)
            return;

        playerSequence.Add(index);

        selectedTexts[playerSequence.Count - 1].text =
            buttons[index].GetComponentInChildren<Text>().text;

        if (playerSequence.Count == correctSequence.Length)
        {
            onComplete.Invoke();
        }
    }

    public void CheckSequence()
    {
        bool success = true;

        for (int i = 0; i < correctSequence.Length; i++)
        {
            if (playerSequence[i] != correctSequence[i])
            {
                success = false;
                break;
            }
        }

        if (success)
            onSuccess.Invoke();
        else
            onFail.Invoke();
    }

    public void ResetSequence()
    {
        playerSequence.Clear();
        foreach (Text text in selectedTexts)
    {
        text.text = "";
    }
    }
}