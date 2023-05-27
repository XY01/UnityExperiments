using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;
using Unity.Collections;
using Random = UnityEngine.Random;
using UnityEngine.Rendering;
using System;



// PROFILING
// 128
// Sort time    0.0003441
// Get Data     0.0005473

// 1024
// Sort time    0.0004013
// Get Data     0.0011548

public class ComputeSort : MonoBehaviour
{
    [System.Serializable]
    public struct PosDistData
    {
        public float3 pos;
        public float dist;
    }

    public enum ReadbackMode
    {
        MainThreadBlocking,
        Async
    }

    public Vector3 positionToCalcDistFrom;
    public ReadbackMode readbackMode = ReadbackMode.MainThreadBlocking;

    // Numthread needs to match in the compute shader
    // to make sure the thread count is the same. 
    // Max threads is 1024
    const int THREAD_NUM = 1024;

    // Shader and kernel indecies
    public ComputeShader sortComputeShader;
    private int bitonicSortKernel;
    private int computeDistancesKernel;
    AsyncGPUReadbackRequest request;
    bool asyncRequestActive = false;
   
    // Data arrays and buffers
    private ComputeBuffer buffer;
    PosDistData[] sortedData;
    NativeArray<PosDistData> sortedDataNative;

    public bool sortEveryFrame = true;
    // Verification to make sure the arrays have been sorted correctly
    public bool runVerify = true;

    int profilingFrameStart = 0;

    void Start()
    {
        Debug.Log("System supports async GPU readback: " + SystemInfo.supportsAsyncGPUReadback);

        // Get kernel indicies
        bitonicSortKernel = sortComputeShader.FindKernel("BitonicSort");
        computeDistancesKernel = sortComputeShader.FindKernel("ComputeDistances");

        // Create data array and fill it with values
        sortedData = new PosDistData[THREAD_NUM];       
        for (int i = 0; i < sortedData.Length; i++)
            sortedData[i].pos = Vector3.one * Mathf.Floor(Random.value * THREAD_NUM);
        
        sortedDataNative = new NativeArray<PosDistData>(sortedData, Allocator.Persistent);

        // Create buffer and set it on the shader
        buffer = new ComputeBuffer(sortedData.Length, Marshal.SizeOf(typeof(PosDistData)));
        buffer.SetData(sortedDataNative);

        // Set shader variables
        sortComputeShader.SetBuffer(bitonicSortKernel, "posDistData", buffer);
        sortComputeShader.SetBuffer(computeDistancesKernel, "posDistData", buffer);
    }


    void Update()
    {
        if (sortEveryFrame)        
            RunSort();        
    }
       
    [ContextMenu("Sort")]
    void RunSort()
    {
        if (asyncRequestActive)
            return;

        profilingFrameStart = Time.frameCount;

        // Set testPos on the shader
        sortComputeShader.SetVector("testPos", positionToCalcDistFrom);

        // Run the compute shader
        sortComputeShader.Dispatch(computeDistancesKernel, buffer.count / THREAD_NUM, 1, 1);

        // Run bittonic sort in varying sizes so the entire array can be sorted
        int numThreadGroups = (buffer.count + THREAD_NUM - 1) / THREAD_NUM;
        for (uint size = 2; size <= THREAD_NUM; size <<= 1)
        {
            for (uint stride = size / 2; stride > 0; stride >>= 1)
            {
                sortComputeShader.SetInt("Size", (int)size);
                sortComputeShader.SetInt("Stride", (int)stride);
                sortComputeShader.Dispatch(bitonicSortKernel, numThreadGroups, 1, 1);
            }
        }       

        if(readbackMode == ReadbackMode.Async)
        {
            // Async readback doesn't block main thread and runs the callback when recieved
            request = AsyncGPUReadback.RequestIntoNativeArray(ref sortedDataNative, buffer, Callback);
            asyncRequestActive = true;
        }
        else
        {
            // Get data blocks main thread until complete      
            buffer.GetData(sortedData);
          
            if (runVerify)
                Verify();
        }
    }

    private void Callback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError) throw new Exception("AsyncGPUReadback.RequestIntoNativeArray");

        if (runVerify)
            Verify();

        asyncRequestActive = false;
    }

    public void Verify()
    {
        if (readbackMode == ReadbackMode.MainThreadBlocking)
        {
            for (int i = 0; i < sortedData.Length - 1; i++)
            {
                if (sortedData[i].dist < sortedData[i + 1].dist)
                {
                    Debug.Log($"Not sorted {i} {sortedData[i].dist} !< {sortedData[i + 1].dist}");
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < sortedDataNative.Length - 1; i++)
            {
                if (sortedDataNative[i].dist < sortedDataNative[i + 1].dist)
                {
                    Debug.Log($"Not sorted {i} {sortedDataNative[i].dist} !< {sortedDataNative[i + 1].dist}");
                    break;
                }
            }
        }

        int frameCount = Time.frameCount - profilingFrameStart;
        Debug.Log("Sort verified. Readback mode: " + readbackMode + "  frame count: " + frameCount);
    }

    void OnDestroy()
    {
        // Make sure to release compute buffer and dispose of native arrays
        buffer?.Release();
        sortedDataNative.Dispose();
    }
}
