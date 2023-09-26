using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class BingoCard : MonoBehaviour
{
    public GameObject[] cardCells;
    public Texture2D[] cardTextures;

    [ContextMenu("Generate card")]
    void GenerateRandomCard()
    {
        List<GameObject> bingoCellList = new();
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject go = transform.GetChild(i).gameObject;
            bingoCellList.Add(transform.GetChild(i).gameObject);
        }

        cardCells = bingoCellList.ToArray();
        
        List<int> numbers = new List<int>();
        
        // Prepare possible numbers for each column
        List<int>[] columns = new List<int>[5];
        for (int i = 0; i < 5; i++)
        {
            columns[i] = new List<int>();
            for (int j = 1; j < 15; j++)
            {
                columns[i].Add(j + i * 15);
            }
        }
        
        // Shuffle numbers for each column
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int randomIndex = Random.Range(0, columns[i].Count);
                numbers.Add(columns[i][randomIndex]);
                columns[i].RemoveAt(randomIndex);
            }
        }
        
        // Set numbers on the card
        for (int i = 0; i < cardCells.Length; i++)
        {
            //cardCells[i].GetComponentInChildren<TextMeshProUGUI>().text = numbers[i].ToString();
            RawImage rawImage = cardCells[i].GetComponentInChildren<RawImage>();
            //Debug.LogError(i + "   " + numbers.Count);
            int index = numbers[i];
            index = Mathf.Min(index, cardTextures.Length - 1);
            rawImage.texture = cardTextures[index];
        }
    }

    private int cardIndex = 0;
    [ContextMenu("Capture")]
    void CaptureScreenshot()
    {
        ScreenCapture.CaptureScreenshot($"Card{cardIndex}.png");
        cardIndex++;
    }

    [ContextMenu("Capture cards")]
    void CaptureXCards()
    {
        StartCoroutine(CaptureCardsRoutine());
    }

    public int numToCapture = 30;
    IEnumerator CaptureCardsRoutine()
    {
        int count = 0;
        while (count < numToCapture)
        {
            GenerateRandomCard();
            CaptureScreenshot();
            yield return new WaitForEndOfFrame();
            count++;
        }
    }
}