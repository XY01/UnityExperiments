using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FoamBehviorMono : MonoBehaviour
{
    private float[,] pressureSource;
    private float[,] pressureResult;
    
    private Vector2[,] velocitiesSource;
    private Vector2[,] velocitiesResult;
    
    public float Viscosity;
    public int Width = 30;
    public int Height = 30;

    private int2[] neighbourhood =  new int2[]{
        new int2(-1, 0),
        new int2(1, 0),
        new int2(0, -1),
        new int2(0, 1)
    };

    // Start is called before the first frame update
    void Start()
    {
        pressureSource = new float[Width, Height];
        pressureResult = new float[Width, Height];
        velocitiesSource = new Vector2[Width, Height];
        velocitiesResult = new Vector2[Width, Height];
        
        Reset();
    }

    void Reset()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                pressureResult[x, y] = (x > Width*.4f && x < Width*.6f &&
                                        y > Height*.4f && y < Height*.6f )
                                    ? 1 : 0;

                pressureSource[x,y] = pressureResult[x, y];
                velocitiesResult[x, y] = Vector2.zero;
                velocitiesSource[x, y] = Vector2.zero;
            }
        }
    }

    // Update is called once per frame
    public bool updateManually = false;
    void Update()
    {
        float timeStep = Time.deltaTime;
        if (updateManually)
        {
            timeStep = Input.GetKeyDown(KeyCode.S) ? .2f : 0;
            
            if(timeStep == 0)
                return;
        }
       
        for (int x = 1; x < Width-1; x++)
        {
            for (int y = 1; y < Height-1; y++)
            {
                UpdateCell(x, y, timeStep);
            }
        }

        BufferSwap();
    }

    void BufferSwap()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                pressureSource[x,y] = pressureResult[x, y];
                velocitiesSource[x,y] = velocitiesResult[x, y];
            }
        }
    }

    void UpdateCell(int x, int y, float timeStep)
    {
        float2 vel = velocitiesSource[x,y];
        
        float2 velocityLeft = velocitiesSource[x-1, 0];
        float2 velocityRight = velocitiesSource[x+1, 0];
        float2 velocityDown = velocitiesSource[x,y -1];
        float2 velocityUp = velocitiesSource[x, y+1];
        
        float pressureLeft = pressureSource[x-1,0];
        float pressureRight = pressureSource[x+1,0];
        float pressureDown = pressureSource[x,y-1];
        float pressureUp = pressureSource[x,y+1];
        
        
        // Calculate pressure gradient
        float2 pressureGradient = new float2(pressureRight - pressureLeft, pressureUp - pressureDown) * 0.5f;
        
        // Calculate viscosity term
        float2 viscosityTerm = Viscosity * (velocityRight + velocityLeft + velocityUp + velocityDown - 4.0f * vel);
        
        // Update velocity
        velocitiesResult[x,y] = vel - timeStep * (pressureGradient + viscosityTerm);
        
        // Update pressure
        float divergence = (velocityRight.x - velocityLeft.x + velocityUp.y - velocityDown.y) * 0.5f;
        pressureResult[x,y] = 0.5f * (pressureLeft + pressureRight + pressureDown + pressureUp) - 0.5f * divergence;
    }
    
    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
            return;
        
        float cellSize = .1f;
        float offset = -(Width / 2f)*cellSize;
       
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Gizmos.DrawWireCube(
                    new Vector3((x * cellSize) + offset,cellSize*.5f, (y * cellSize) + offset),
                    Vector3.one * cellSize);
                
                Gizmos.DrawCube(
                    new Vector3((x * cellSize) + offset,cellSize*.5f, (y * cellSize) + offset),
                    Vector3.one * pressureResult[x,y] * cellSize * .9f);
            }
        }
    }
}

/*
 
#pragma kernel CSMain
RWTexture2D<float2> VelocityResult;
RWTexture2D<float> PressureResult;

Texture2D<float2> PreviousVelocity;
Texture2D<float> PreviousPressure;
float TimeStep;
float Viscosity;
int Width;
int Height;
// These are needed to calculate the gradients and divergence
int2 Neighbourhood[4] = {
    int2(-1, 0),
    int2(1, 0),
    int2(0, -1),
    int2(0, 1)
};
[numthreads(8,8,1)]
void CSMain (uint3 id : SV_DispatchThreadID)
{
    float2 uv = id.xy / float2(Width, Height);
    float2 currentVelocity = PreviousVelocity[uv];
    float currentPressure = PreviousPressure[uv];
    // Calculate velocity divergence and pressure gradient
    float divergence = 0;
    float2 gradient = 0;
    for(int i=0; i<4; i++)
    {
        float2 neighbourVelocity = PreviousVelocity[id.xy + Neighbourhood[i]];
        float neighbourPressure = PreviousPressure[id.xy + Neighbourhood[i]];
        divergence += neighbourPressure;
        gradient -= neighbourVelocity * neighbourPressure;
    }
    // Update velocity and pressure based on their gradients and the timestep
    float2 newVelocity = currentVelocity + TimeStep * (Viscosity * divergence + gradient);
    float newPressure = currentPressure + TimeStep * dot(newVelocity, divergence);
    // Write the result back into the texture
    VelocityResult[id.xy] = newVelocity;
    PressureResult[id.xy] = newPressure;
}

 */
