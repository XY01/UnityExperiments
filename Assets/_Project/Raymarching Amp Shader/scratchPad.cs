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
        float denisty = 0;
        float transmission = 0;
        float lightAccumulation = 0;
        float finalLight = 0;
        
        // Distance
        float totalDist = 0;
        float distToDepth = length(rayOrigin - depthPos);
        for (int i = 0; i < numSteps; i++)
        {
            rayOrigin += rayDirection * stepSize;
            totalDist += stepSize;
            // if (totalDist >= distToDepth)
            //     break;
            
            float3 sampleRay = rayOrigin + offset;
            float3 samplePosMip = float3(sampleRay);
        
            float sampleDensity = tex3D(Volume, samplePosMip).r;
            denisty += sampleDensity * densityScale;
        
            // Lighting
            float3 lightRayOrigin = samplePosMip;
            for (int j = 0; j < numLightSteps; j++)
            {
                lightRayOrigin += lightDir * lightStepSize;
                float lightDensity = tex3D(Volume, samplePosMip).r;
                lightAccumulation += lightDensity * densityScale;
            }
        
            float lightTransmission = exp(-lightAccumulation);
            float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
            finalLight += denisty * transmittance * shadow;
            transmittance *= exp(-denisty * lightAsorb);
        }
        
        transmission = exp(-denisty);
        return float3(finalLight, transmission, transmittance);
        
        
        
        float denisty = 0;
        float transmission = 0;
        float lightAccumulation = 0;
        float finalLight = 0;
        
        // Distance
        float totalDist = 0;
        float distToDepth = length(rayOrigin - depthPos);

        if (length(viewPos - rayOrigin) > length(viewPos - depthPos))
            return float3(0,0,0);
        
        for (int i = 0; i < numSteps; i++)
        {
            if (totalDist > distToDepth)
                break;
            
            totalDist += stepSize;
            rayOrigin += rayDirection * stepSize;

            float3 samplingPos = rayOrigin+offset;
            float4 samplePosMip = float4(samplingPos, 0);
            
            float sampleDensity = tex3Dlod(Volume,samplePosMip).r;
            denisty += sampleDensity * densityScale;

            // Lighting
            float3 lightRayOrigin = samplingPos;
            for (int j = 0; j < numLightSteps; j++)
            {
                lightRayOrigin += lightDir * lightStepSize;
                float4 lightRayOriginMip = float4(lightRayOrigin,0);
                float lightDensity = tex3Dlod(Volume, lightRayOriginMip).r;
                lightAccumulation += lightDensity * densityScale; 
            }

            float lightTransmission = exp(-lightAccumulation);
            float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
            finalLight += denisty * transmittance * shadow;
            transmittance *= exp(-denisty*lightAsorb);
        }

        transmission = exp(-denisty);
        return float3(finalLight, transmission, transmittance);
    }
}
