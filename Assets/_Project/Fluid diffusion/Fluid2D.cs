using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public class Fluid2D : MonoBehaviour
{
    public const int dimensions = 32;

    float[,] density = new float[dimensions, dimensions];   
    Vector2[,] velocities = new Vector2[dimensions, dimensions];

    float[,] nextDensity = new float[dimensions, dimensions];
    Vector2[,] velocities0 = new Vector2[dimensions, dimensions];


    public float densityDiffusionRate = 1;
    public float advectionRate = 1;

    Vector3 mouseVel;
    Vector3 prevMousePos;
    public Vector3 mousePos;

    public float debugDensityAdd = 1f;

    public bool newMethod = true;


    [ContextMenu("Start")]
    // Start is called before the first frame update
    void Start()
    {
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                float xNorm = x / (float)dimensions;
                xNorm = 1 -  Mathf.Abs(.5f - xNorm) * 2;
                float yNorm = y / (float)dimensions;
                yNorm = 1 -  Mathf.Abs(.5f - yNorm) * 2;

                if (x == 0 || x == dimensions - 1 ||
                    y == 0 || y == dimensions - 1)
                {
                    density[x, y] = 0;
                    nextDensity[x, y] = 0;
                }
                else
                {
                    density[x, y] = xNorm * yNorm;
                    nextDensity[x, y] = xNorm * yNorm;
                }

                //velocities[x, y] = Random.insideUnitCircle;
                //velocities[x, y] = Vector2.right * Random.value;
            }
        }
    }


    void Reset()
    {
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                density[x, y] = 0;
                nextDensity[x, y] = 0;
                velocities[x, y] = Vector2.zero;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.D))
        {
            if(!newMethod)
                Diffusion(density, nextDensity, Time.fixedDeltaTime, densityDiffusionRate);
            else
                DiffuseNew(density, nextDensity, densityDiffusionRate, Time.fixedDeltaTime);
        }

        if (Input.GetKey(KeyCode.A))
            Advection(Time.fixedDeltaTime * advectionRate);

        if (Input.GetKey(KeyCode.R))
            Reset();


        // MOUSE INTERACTION
        //
        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = -Camera.main.transform.position.z; //distance of the plane from the camera
        mousePos = Camera.main.ScreenToWorldPoint(screenPoint);

        Vector3 newMouseVel = (mousePos - prevMousePos)/Time.deltaTime;
        newMouseVel *= .3f;
        mouseVel = Vector3.Lerp(mouseVel, newMouseVel, Time.deltaTime * 8);

        prevMousePos = mousePos;

        //velocities[(int)dimensions/2, (int)dimensions / 2] = Vector2.up;
        //density[(int)dimensions / 2, (int)dimensions / 2] += Time.deltaTime * debugDensityAdd;

        // CHANGE VEL
        if (Input.GetMouseButton(0))
        {
            Vector2 coord = GetGridCoord(mousePos);
            velocities[(int)coord.x, (int)coord.y] = mouseVel;
        }
        // ADD DENSITY
        if (Input.GetMouseButton(1))
        {
            Vector2 coord = GetGridCoord(mousePos);
            density[(int)coord.x, (int)coord.y] += Time.deltaTime * 10;
        }
    }


    void DiffuseNew(float[,] x, float[,] x0, float diffusionStrength, float dt)
    {
        float a = dt * diffusionStrength;
        linearSolver(density, nextDensity, a, dt, 1);        
    }


    void Diffusion(float[,] current, float[,] target,  float timeStep, float diffusionRate)
    {
        float recip = 1f / timeStep;
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                float currentDensity = current[x, y];
                float left = currentDensity;
                float right = currentDensity;
                float up = currentDensity;
                float down = currentDensity;

                if (x > 0)                
                    left = current[x - 1, y];
                
                if (x < (dimensions - 1))                
                    right = current[x + 1, y];
                
                if (y > 0)                
                    down = current[x, y - 1];
                
                if (y < (dimensions - 1))                
                    up = current[x, y + 1];

                float neighborAggregate = (left + right + up + down) / 4f;
                target[x, y] = neighborAggregate;
                //target[x,y] = (current[x, y] + (diffusionRate * neighborAggregate)) / (1 + timeStep);
            }
        }

        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                current[x, y] = (current[x, y] + (timeStep * target[x, y])) / (1 + timeStep);
            }
        }
        //InterpolateDensityBuffers(timeStep);
    }


    public void linearSolver(float[,] array, float[,] arrayPrev, float strength, float timestep, int iterations)
    {
        float cRecip = 1f / timestep;
        int xDimensions = array.GetUpperBound(0) - 1;
        int yDimensions = array.GetUpperBound(1) - 1;
        for (int t = 0; t < iterations; t++)
        {
            for (int x = 1; x < xDimensions; x++)
            {
                for (int y = 1; y < yDimensions; y++)
                {
                    float neighborAggregate = array[x + 1, y] +
                                                array[x - 1, y] +
                                                array[x, y + 1] +
                                                array[x, y - 1];

                    array[x, y] = arrayPrev[x, y] + strength * neighborAggregate * cRecip;
                }
            }

            //FluidHelpers.SetBoundaries(true, true, array, dimensions);
           
        }
    }

    //void Diffusion(float timeStep)
    //{
    //    for (int x = 0; x < dimensions; x++)
    //    {
    //        for (int y = 0; y < dimensions; y++)
    //        {
    //            float currentDensity = density[x, y];
    //            float left = currentDensity;
    //            float right = currentDensity;
    //            float up = currentDensity;
    //            float down = currentDensity;

    //            Vector2 leftVel = velocities[x, y];
    //            Vector2 rightVel = velocities[x, y];
    //            Vector2 upVel = velocities[x, y];
    //            Vector2 downVel = velocities[x, y];

    //            if (x > 0)
    //            {
    //                left = density[x - 1, y];
    //                leftVel = velocities[x - 1, y];
    //            }
    //            if (x < (dimensions - 1))
    //            {
    //                right = density[x + 1, y];
    //                rightVel = velocities[x + 1, y];
    //            }
    //            if (y > 0)
    //            {
    //                down = density[x, y - 1];
    //                downVel = velocities[x, y - 1];
    //            }
    //            if (y < (dimensions - 1))
    //            {
    //                up = density[x, y + 1];
    //                upVel = velocities[x, y + 1];
    //            }

    //            targetDensity[x, y] = (left + right + up + down) / 4f;
    //            targetVelocities[x, y] = (leftVel + rightVel + upVel + downVel) / 4f;

    //            // DIR AWAY FROM DENSITY
    //            //float xDir = (right - left)/2;
    //            //float yDir = (up - down)/2;
    //            //targetVelocities[x, y] = new Vector2(xDir, yDir);
    //        }
    //    }

    //    InterpolateDensityBuffers(timeStep);
    //}

    //Vector2[,] GradientField()
    //{
    //    for (int x = 0; x < dimensions; x++)
    //    {
    //        for (int y = 0; y < dimensions; y++)
    //        {
    //        }
    //    }
    //}

    // Movement of denisty through the vectorfield

    void Advection(float timeStep)
    {
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                float xSubVelx = x - velocities[x, y].x * timeStep;
                float ySubVely = y - velocities[x, y].y * timeStep;
                int xIndex = Mathf.FloorToInt(xSubVelx);
                xIndex = Mathf.Clamp(xIndex, 0, dimensions - 2);
                int yIndex = Mathf.FloorToInt(ySubVely);
                yIndex = Mathf.Clamp(yIndex, 0, dimensions - 2);
                float xInterpolation = xSubVelx - xIndex;
                float yInterpolation = ySubVely - yIndex;

                // DENSITY
                float lerpedUpperDensity = Mathf.Lerp(density[xIndex, yIndex], density[xIndex + 1, yIndex], xInterpolation);
                float lerpedLowerDensity = Mathf.Lerp(density[xIndex, yIndex + 1], density[xIndex + 1, yIndex + 1], xInterpolation);
                nextDensity[x,y] = Mathf.Lerp(lerpedUpperDensity, lerpedLowerDensity, yInterpolation);

                // VELOCITY
                //Vector2 lerpedUpperVel = Vector2.Lerp(velocities[xIndex, yIndex], velocities[xIndex + 1, yIndex], xInterpolation);
                //Vector2 lerpedLowerVel = Vector2.Lerp(velocities[xIndex, yIndex + 1], velocities[xIndex + 1, yIndex + 1], xInterpolation);
                //targetVelocities[x, y] = Vector2.Lerp(lerpedUpperVel, lerpedLowerVel, yInterpolation);
                //targetVelocities[x, y] = velocities[x, y];
            }
        }

        InterpolateDensityBuffers(timeStep);
    }

    void InterpolateDensityBuffers(float timeStep)
    {        
        float totalVolume = 0;
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                density[x, y] = (density[x, y] + (timeStep * nextDensity[x, y])) / (1 + timeStep);
                //velocities[x, y] = (velocities[x, y] + (timeStep * targetVelocities[x, y])) / (1 + timeStep);
                totalVolume += density[x, y];
            }
        }

        print("Total volume: " + totalVolume);
    }

    void CalcDivergence()
    {
        for (int x = 1; x < dimensions-1; x++)
        {
            for (int y = 1; y < dimensions-1; y++)
            {
                float divergence = (velocities[x + 1, y].x - velocities[x - 1, y].x + velocities[x, y + 1].y - velocities[x, y - 1].y)/2;
            }
        }
    }


  

    Vector2 GetGridCoord(Vector3 worldPos)
    {
        Vector2 index = new Vector2(0,0);
        float size = 10f;
        float cellSize = size / (float)dimensions;
        Vector3 startPos = new Vector3(-size * .5f, -size * .5f, 0);

        worldPos -= startPos;
        index.x = Mathf.Clamp(worldPos.x / cellSize, 0, dimensions-1);
        index.y = Mathf.Clamp(worldPos.y / cellSize, 0, dimensions - 1);

        return index;
    }

    private void OnDrawGizmos()
    {        
        float size = 10f;
        float cellSize = size / (float)dimensions;
        Vector3 startPos = new Vector3(-size * .5f, -size * .5f, 0);
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                Vector3 pos = startPos + new Vector3(x * cellSize, y * cellSize, 0);

                // DENISTY
                Gizmos.color = Color.white * density[x, y];
                Gizmos.DrawCube(pos, Vector3.one * cellSize);


                // VECTORFIELD
                Gizmos.color = Color.white * .2f;
               Vector3 vel = new Vector3
                (
                    velocities[x, y].x * cellSize,
                    velocities[x, y].y * cellSize,
                    0
                );
                Gizmos.DrawLine(pos, pos + vel);
            }
        }

        // MOUSE
        //
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(mousePos, cellSize * .25f);
        Gizmos.DrawLine(mousePos, mousePos + (mouseVel * cellSize));
    }
}
