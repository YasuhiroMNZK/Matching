using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomTextGenerator : MonoBehaviour
{
    [Header("sourceTexts")]
    [TextArea]
    public string[] sourceTexts;

    [Header("targetText")]
    public Text[] targetTexts;
    private void Start()
    {
        RandomizeTexts();
    }
    void RandomizeTexts()
    {
        if (sourceTexts.Length < targetTexts.Length)
        {
            return;
        }


        List<string> pool = new List<string>(sourceTexts);


        for (int i = 0; i < targetTexts.Length; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);

            targetTexts[i].text = pool[randomIndex];


            pool.RemoveAt(randomIndex);
        }
    }
}