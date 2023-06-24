using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class scratchPad : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int numSteps;
        float3 rayOrigin;
        float3 rayDirection;
        float stepSize;
        
        
        float denisty;
        
        for (int i = 0; i < numSteps; i++)
        {
            rayOrigin += rayDirection * stepSize;

            float sampleDensity = tex3D(Volume, rayOrigin).r;
            denisty += sampleDensity * densityScale;

        }
        
        return denisty;
    }
}
