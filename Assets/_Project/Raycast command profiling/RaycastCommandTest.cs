using System;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = System.Diagnostics.Debug;
using Random = UnityEngine.Random;

public class RaycastCommandTest : MonoBehaviour
{
    public int rayCount = 1000;
    public int maxHits = 1;
    private Vector3 origin = Vector3.forward * -10;

    private void Start()
    {
        if (!allocateNativeArraysOnCall)
        {
            results = new NativeArray<RaycastHit>(rayCount * maxHits, Allocator.Persistent);
            commands = new NativeArray<RaycastCommand>(rayCount, Allocator.Persistent);
            
            for (int i = 0; i < rayCount; i++)
            {
                commands[i] = new RaycastCommand(origin, RandomDirection(), QueryParameters.Default);
            }
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
            RunRaycastCommand();

        if (Input.GetKeyDown(KeyCode.T))
            RunRaycastMono();
    }

    // Testing notes - allocating each call
    // 1000 - .56ms
    // 10000 - 5.4ms
    
    // After removing vector normalization
    // 500 - .21ms
    // 1000 - .42ms
    // 10000 - 3.84ms + 
    private void RunRaycastMono()
    {
        //StartStopWatch();
        Profiler.BeginSample("Raycast Mono");
        int hitCount = 0;
        int rayCountExecuted = 0;
        for (int i = 0; i < rayCount; i++)
        {
            rayCountExecuted++;
            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(origin, RandomDirection(), out hit))
            {
                hitCount++;
            }
        }
        Profiler.EndSample();
        
        //StopStopWatch("RunRaycastMono");
        UnityEngine.Debug.Log($"hit Count {hitCount}   rayCountExecuted {rayCountExecuted}");
    }

    // Testing notes - allocating each call
    // 1000 - .48ms
    // 10000 - 3.5ms
    // Makes no difference allocating each frame or not
    
    // Removed vector normalization which seemed to eat up a big chunk, which could be done in a job as well
    // 500 - .21ms
    // 1000 - .34ms
    // 10000 - 2.5ms
    Vector3 RandomDirection()
    {
        Vector3 randFwd = Random.onUnitSphere;// Vector3.forward * 5 + Random.insideUnitSphere * 10;
        return randFwd;
    }

    public bool allocateNativeArraysOnCall = true;
    private NativeArray<RaycastHit> results;
    private NativeArray<RaycastCommand> commands;
    private void RunRaycastCommand()
    {
        Profiler.BeginSample("Raycast Command");

        int hitCount = 0;
        int rayCountExecuted = 0;
        // Perform a single raycast using RaycastCommand and wait for it to complete
        // Setup the command and result buffers
        // length of the results 
        if (allocateNativeArraysOnCall)
        {
            results = new NativeArray<RaycastHit>(rayCount * maxHits, Allocator.TempJob);
            commands = new NativeArray<RaycastCommand>(rayCount, Allocator.TempJob);
        }
        
        for (int i = 0; i < rayCount; i++)
        {
            commands[i] = new RaycastCommand(origin, RandomDirection(), QueryParameters.Default);
        }

        // Schedule the batch of raycasts.
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, maxHits, default(JobHandle));

        // Wait for the batch processing job to complete
        handle.Complete();

        // Copy the result. If batchedHit.collider is null there was no hit
        foreach (var hit in results)
        {
            rayCountExecuted++;
            if (hit.collider != null)
            {
                // If hit.collider is not null means there was a hit
                hitCount++;
            }
        }
        
        Profiler.EndSample();

        // Dispose the buffers
        if (allocateNativeArraysOnCall)
        {
            results.Dispose();
            commands.Dispose();
        }

        UnityEngine.Debug.Log($"hit Count {hitCount}   rayCountExecuted {rayCountExecuted}");
    }

    private void OnDestroy()
    {
        results.Dispose();
        commands.Dispose();
    }
}