using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidDiffusion1D : MonoBehaviour
{
    const int sampleCount = 32;

    float[] currentSamples = new float[sampleCount];
    float[] newSamples = new float[sampleCount];

    [ContextMenu("Start")]
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < sampleCount; i++)
        {
            currentSamples[i] = Random.value;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // LAPLACIAN DIFFUSION
        //
        if (Input.GetKey(KeyCode.R))
            LapDiff1D();
    }

    [ContextMenu("Lapdiff 1D")]
    void LapDiff1D()
    {
        for (int x = 0; x < sampleCount; x++)
        {
            float px = currentSamples[x];
            float neg_px = px;
            float pos_px = px;

            if (x > 0)
            {
                neg_px = currentSamples[x - 1];
            }
            if (x < (sampleCount - 1))
            {
                pos_px = currentSamples[x + 1];
            }

            float dx = (neg_px + pos_px) / 2;

            newSamples[x] = dx;
        }

        InterpolateBuffers();
    }

    void InterpolateBuffers()
    {
        float timestep = Time.fixedDeltaTime * .01f;
        float totalVolume = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            currentSamples[i] = Mathf.MoveTowards(currentSamples[i], newSamples[i], timestep);
            totalVolume += currentSamples[i];
        }

        print("Total volume: " + totalVolume);
    }

    void OnDrawGizmos()
    {
        float width = 10;
        float height = 1;

        float widthPerSample = width / (float)sampleCount;
        for (int i = 0; i < sampleCount; i++)
        {
            float sampleHeight = currentSamples[i] * height;
            Vector3 pos = new Vector3(i * widthPerSample - width * .5f, sampleHeight * .5f, 0);
            Gizmos.DrawWireCube(pos, new Vector3(widthPerSample, sampleHeight, widthPerSample));
        }  
    }
}
