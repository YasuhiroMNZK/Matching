using UnityEngine;
using UnityEngine.Events;

public class StepEventManager : MonoBehaviour
{
    [Header("当前步骤")]
    public int count = 0;

    [Header("0~5对应事件")]
    public UnityEvent step0;
    public UnityEvent step1;
    public UnityEvent step2;
    public UnityEvent step3;
    public UnityEvent step4;
    public UnityEvent step5;

    // 调用一次，执行当前事件，然后count+1
    public void NextStep()
    {
        switch (count)
        {
            case 0:
                step0.Invoke();
                break;
            case 1:
                step1.Invoke();
                break;
            case 2:
                step2.Invoke();
                break;
            case 3:
                step3.Invoke();
                break;
            case 4:
                step4.Invoke();
                break;
            case 5:
                step5.Invoke();
                break;
        }

        count++;
    }

    // 重置
    public void ResetCount()
    {
        count = 0;
    }
}