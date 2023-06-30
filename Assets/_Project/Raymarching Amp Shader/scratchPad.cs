// using System.Collections;
// using System.Collections.Generic;
// using Unity.Mathematics;
// using Unity.VisualScripting;
// using UnityEngine;
// using UnityEngine.Rendering;

// public class scratchPad : MonoBehaviour
// {
    // float denisty = 0;
    // float transmission = 0;
    // float lightAccumulation = 0;
    // float finalLight = 0;
    //     
    // // Distance
    // float totalDist = 0;
    // float distToDepth = length(rayOrigin - depthPos);
    // float volumeScale = .5;
    //  
    // for (int i = 0; i < numSteps; i++)
    // {
    //     if (totalDist > distToDepth)
    //         break;
    //         
    //     totalDist += stepSize;
    //     rayOrigin += rayDirection * stepSize;
    //
    //     float3 samplingPos = rayOrigin+offset;
    //     float4 samplePosMip = float4(samplingPos, 0);
    //     
    //     // Sample noise to offset ray/uv
    //     float4 noiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
    //     float noiseSample = pow(tex3Dlod(Noise3D,noiseSamplePos).r,noiseStr);
    //     
    //     // Sample smoke volume tex
    //     float noiseLookupOffset = noiseSample * normalize(noiseOffset) * .1 * noiseStr;
    //     samplePosMip.xyz += noiseLookupOffset;
    //     float sampleDensity = tex3Dlod(Volume,samplePosMip).r;
    //     
    //     // Scale the noise density contribution
    //     sampleDensity *= noiseSample * noiseDensityScalar;
    //     denisty += sampleDensity * densityScale;
    //     
    //     
    //
    //     // Lighting
    //     float3 lightRayOrigin = samplingPos;
    //     for (int j = 0; j < numLightSteps; j++)
    //     {
    //         lightRayOrigin += lightDir * lightStepSize;
    //         float4 lightRayOriginMip = float4(lightRayOrigin,0);
    //         
    //         // Sample noise
    //         float4 lightNoiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
    //         float lightNoiseSample = pow(tex3Dlod(Noise3D,lightNoiseSamplePos).r,noiseStr);
    //         
    //         // Sample volume tex
    //         noiseLookupOffset = lightNoiseSample * normalize(noiseOffset) * .1 * noiseStr;
    //         lightRayOriginMip.xyz += noiseLookupOffset;
    //         float lightDensity = tex3Dlod(Volume, lightRayOriginMip).r;
    //         
    //         // Scale the noise density contribution
    //         lightDensity  *= lightNoiseSample * noiseDensityScalar;
    //         lightAccumulation += lightDensity * densityScale; 
    //     }
    //
    //     float lightTransmission = exp(-lightAccumulation);
    //     float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
    //     finalLight += denisty * transmittance * shadow;
    //     transmittance *= exp(-denisty*lightAsorb);
    // }
    //
    // transmission = exp(-denisty);
    // return float3(finalLight, transmission, transmittance);
//}


//
// /// WORKING
// /// float denisty = 0;
// float transmission = 0;
// float lightAccumulation = 0;
// float finalLight = 0;
//         
// // Distance
// float totalDist = 0;
// float distToDepth = length(rayOrigin - depthPos);
//
//      
// for (int i = 0; i < numSteps; i++)
// {
//     if (totalDist > distToDepth)
//         break;
//             
//     totalDist += stepSize;
//     rayOrigin += rayDirection * stepSize;
//
//     float3 samplingPos = rayOrigin+offset;
//     float4 samplePosMip = float4(samplingPos, 0);
//             
//     float sampleDensity = tex3Dlod(Volume,samplePosMip).r;
//     samplePosMip.xyz += noiseOffset;
//     float noiseSample = pow(( tex3Dlod(Noise3D,samplePosMip).r),noiseStr);
//     sampleDensity *= noiseSample;
//     denisty += sampleDensity * densityScale;
//         
//         
//
//     // Lighting
//     float3 lightRayOrigin = samplingPos;
//     for (int j = 0; j < numLightSteps; j++)
//     {
//         lightRayOrigin += lightDir * lightStepSize;
//         float4 lightRayOriginMip = float4(lightRayOrigin,0);
//         float lightDensity = tex3Dlod(Volume, lightRayOriginMip).r;
//         lightRayOriginMip.xyz += noiseOffset;
//         float lightNoiseSample = pow(( tex3Dlod(Noise3D,lightRayOriginMip).r),noiseStr);
//         lightDensity *= lightNoiseSample;
//         lightAccumulation += lightDensity * densityScale; 
//     }
//
//     float lightTransmission = exp(-lightAccumulation);
//     float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
//     finalLight += denisty * transmittance * shadow;
//     transmittance *= exp(-denisty*lightAsorb);
// }
//
// transmission = exp(-denisty);
// return float3(finalLight, transmission, transmittance);


   // float denisty = 0;
   //  float transmission = 0;
   //  float lightAccumulation = 0;
   //  float finalLight = 0;
   //      
   //  // Distance
   //  float totalDist = 0;
   //  float distToDepth = length(rayOrigin - depthPos);
   //
   // float3 center = float3(10, 2, 0);
   //   
   //  for (int i = 0; i < numSteps; i++)
   //  {
   //      if (totalDist > distToDepth)
   //          break;
   //          
   //      totalDist += stepSize;
   //      rayOrigin += rayDirection * stepSize;
   //  
   //      float3 samplingPos = rayOrigin+offset +(noiseOffset * .23);
   //      float4 samplePosMip = float4(samplingPos, 0);
   //      
   //      // Sample noise to offset ray/uv
   //      float4 noiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
   //      float noiseSample = pow(tex3Dlod(Noise3D,noiseSamplePos).r,noiseStr);
   //
   //      float radius = 2;
   //      float spherefallOff = 1-saturate(length(rayOrigin - distToDepth)/radius);
   //      
   //      // Sample smoke volume tex
   //      float noiseLookupOffset = noiseSample * normalize(noiseOffset) * .1 * noiseStr;
   //      samplePosMip.xyz += noiseLookupOffset;
   //      float sampleDensity = tex3Dlod(Noise3D,samplePosMip).r;
   //      
   //      // Scale the noise density contribution
   //      sampleDensity *= noiseSample * noiseDensityScalar * spherefallOff;
   //      denisty += sampleDensity * densityScale;
   //      
   //      
   //  
   //      // Lighting
   //      float3 lightRayOrigin = samplingPos;
   //      for (int j = 0; j < numLightSteps; j++)
   //      {
   //          lightRayOrigin += lightDir * lightStepSize;
   //          float4 lightRayOriginMip = float4(lightRayOrigin,0);
   //          
   //          // Sample noise
   //         // float4 lightNoiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
   //         // float lightNoiseSample = pow(tex3Dlod(Noise3D,lightNoiseSamplePos).r,noiseStr);
   //          
   //          // Sample volume tex
   //         // noiseLookupOffset = lightNoiseSample * normalize(noiseOffset) * .1 * noiseStr;
   //         // lightRayOriginMip.xyz += noiseLookupOffset;
   //          float lightDensity = tex3Dlod(Noise3D, lightRayOriginMip).r;
   //          
   //          // Scale the noise density contribution
   //         // lightDensity  *= lightNoiseSample * noiseDensityScalar;
   //          lightAccumulation += lightDensity * densityScale; 
   //      }
   //  
   //      float lightTransmission = exp(-lightAccumulation);
   //      float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
   //      finalLight += denisty * transmittance * shadow;
   //      transmittance *= exp(-denisty*lightAsorb);
   //  }
   //  
   //  transmission = exp(-denisty);
   //  return float3(finalLight, transmission, transmittance);