using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;
using Unity.Collections;
using Random = UnityEngine.Random;
using UnityEngine.Rendering;

public class ComputeSort : MonoBehaviour
{
    const int THREAD_NUM = 1024;
    public ComputeShader shader;

    private ComputeBuffer buffer;
    private int kernel;
    private int computeDistancesKernel;

    Stopwatch stopWatch;

    [SerializeField]  Data[] sortedData;
    NativeArray<Data> sortedDataNative;

    public Vector3 testPos;
    public bool runVerify = true;
    public bool verified = false;

    void Start()
    {
        stopWatch = new Stopwatch();

        kernel = shader.FindKernel("BitonicSort");
        computeDistancesKernel = shader.FindKernel("ComputeDistances");

        // Create data array and fill it with values
        sortedData = new Data[THREAD_NUM];
       
        for (int i = 0; i < sortedData.Length; i++)
        {
            sortedData[i].value = Vector3.one * Mathf.Floor(Random.value * THREAD_NUM);
        }

        sortedDataNative = new NativeArray<Data>(sortedData, Allocator.Persistent);

        // Create buffer and set it on the shader
        buffer = new ComputeBuffer(sortedData.Length, Marshal.SizeOf(typeof(Data)));
        buffer.SetData(sortedDataNative);
        shader.SetBuffer(kernel, "posDistData", buffer);
        shader.SetBuffer(computeDistancesKernel, "posDistData", buffer);
    }

    AsyncGPUReadbackRequest request;
    public bool runContinuous = true;

    public bool asyncGet = false;
    void Update()
    {
        if (runContinuous)
        {
            RunSort();

            if (!asyncGet)
                return;

            // Check if last readback has completed
            if (request.done)
            {
                if (request.hasError)
                {
                    Debug.Log("GPU readback error detected.");
                }
                else
                {
                    sortedDataNative = request.GetData<Data>();
                }
            }
            else
            {
                Debug.Log("Read back successfully");
                // Start new readback
                request = AsyncGPUReadback.Request(buffer);
            }

            if (runVerify)
                Verify();
        }

        Debug.Log(AsyncGPUReadback.supported);

    }

    [ContextMenu("Sort")]
    void RunSort()
    {
        stopWatch.Restart();
        // stopWatch.Start();

        // Set testPos on the shader
        shader.SetVector("testPos", testPos);

        // Run the compute shader
        shader.Dispatch(computeDistancesKernel, buffer.count / THREAD_NUM, 1, 1);


        int numThreadGroups = (buffer.count + THREAD_NUM - 1) / THREAD_NUM;

        for (uint size = 2; size <= THREAD_NUM; size <<= 1)
        {
            for (uint stride = size / 2; stride > 0; stride >>= 1)
            {
                shader.SetInt("Size", (int)size);
                shader.SetInt("Stride", (int)stride);
                shader.Dispatch(kernel, numThreadGroups, 1, 1);
            }
        }
        stopWatch.Stop();



        //Debug.Log("Sort time: " + stopWatch.Elapsed);

        if (!asyncGet)
        {
            // Get the sorted data        
            buffer.GetData(sortedData);

            if (runVerify)
                Verify();
        }

        //Debug.Log("Get data time: " + stopWatch.Elapsed);

      
    }

    // PROFILING
    // 128
    // Sort time    0.0003441
    // Get Data     0.0005473

    // 1024
    // Sort time    0.0004013
    // Get Data     0.0011548

    public void Verify()
    {
        verified = true;
        for (int i = 0; i < sortedData.Length-1; i++)
        {
            if (sortedData[i].distance < sortedData[i+1].distance)
            {
                verified = false;
                Debug.Log($"Not sorted {i} {sortedData[i].distance} !< {sortedData[i+1].distance}");
                break;
            }
        }
    }

    void OnDestroy()
    {
        // Make sure to release the buffer
        if (buffer != null)
            buffer.Release();

        sortedDataNative.Dispose();
    }

    [System.Serializable]
    public struct Data
    {
        public float3 value;
        public float distance;
    }
}
