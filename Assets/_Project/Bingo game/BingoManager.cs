using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BingoManager : MonoBehaviour
{
    public List<int> allNumber = new List<int>();
    public RawImage image;
    public Texture[] images;

    private void Start()
    {
        allNumber = Enumerable.Range(0, 75).ToList();
    }

    public bool randomizing = false;
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
           // if (!randomizing)
            {
               // randomizing = true;
                StartCoroutine(Randomize());
            }
        }
    }

    IEnumerator Randomize()
    {
        float wait = .001f;
        while (wait < .4f)
        {
            int drawnNumber = allNumber[Random.Range(0, allNumber.Count)];
            image.texture = images[drawnNumber];
            yield return new WaitForSeconds(wait);
            wait *= 1.25f;
        }
        DrawNumber();
    }

    public void DrawNumber()
    {
        Debug.Log("Drawn");
        int drawnNumber = allNumber[Random.Range(0, allNumber.Count)];
        allNumber.Remove(drawnNumber);
        image.texture = images[drawnNumber];
    }

}