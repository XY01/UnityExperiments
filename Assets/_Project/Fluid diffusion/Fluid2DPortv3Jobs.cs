using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.VolumeComponent;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using Newtonsoft.Json.Linq;


// REFS
// https://www.dgp.toronto.edu/public_user/stam/reality/Research/pdf/GDC03.pdf
// https://www.youtube.com/watch?v=alhpH6ECFvQ
// https://www.youtube.com/watch?v=qsYE1wMEMPA
// https://www.youtube.com/watch?v=rB83DpBJQsE&t=817s

// TODO 
// - Jobify
// - Walls
// - Height map
public class Fluid2DPortv3Jobs : MonoBehaviour
{
    #region VARIABLES    
    public const int dimensions = 64;
    public float densityDiffusionRate = .0001f;
    public float viscosity = 1;
    public float densityDissipationRate = .5f;

    public float mouseVelScalar = 1;

    //NativeArray<float> density = new float[dimensions * dimensions];
   // NativeArray<float> density0 = new float[dimensions * dimensions];

    NativeArray<float> density;
    NativeArray<float> density0;

    NativeArray<float> velX;
    NativeArray<float> velX0;

    NativeArray<float> velY;
    NativeArray<float> velY0;


    // MOUSE INPUT
    Vector3 mouseVel;
    Vector3 prevMousePos;
    Vector3 mousePos;

    [Header("DEBUG")]
    public float debugDiffScalar = 1;
    #endregion


    static int Index(int x, int y)
    {
        return x + (dimensions * y);
    }

    [ContextMenu("Start")]
    // Start is called before the first frame update
    void Start()
    {
        //for (int x = 0; x < dimensions; x++)
        //{
        //    for (int y = 0; y < dimensions; y++)
        //    {
        //        float xNorm = x / (float)dimensions;
        //        xNorm = 1 -  Mathf.Abs(.5f - xNorm) * 2;
        //        float yNorm = y / (float)dimensions;
        //        yNorm = 1 -  Mathf.Abs(.5f - yNorm) * 2;

        //        if (x == 0 || x == dimensions - 1 ||
        //            y == 0 || y == dimensions - 1)
        //        {
        //            density[Index(x,y)] = 0;
        //            density0[Index(x,y)] = 0;
        //        }
        //        else
        //        {
        //            density[Index(x,y)] = xNorm * yNorm;
        //            density0[Index(x,y)] = xNorm * yNorm;
        //        }
        //    }
        //}

        // FILL ARRAY JOB
        //
        // Create job and init vars
        density = new NativeArray<float>(dimensions * dimensions, Allocator.Persistent);
        density0 = new NativeArray<float>(dimensions * dimensions, Allocator.Persistent);
        velX = new NativeArray<float>(dimensions * dimensions, Allocator.Persistent);
        velX0 = new NativeArray<float>(dimensions * dimensions, Allocator.Persistent);
        velY = new NativeArray<float>(dimensions * dimensions, Allocator.Persistent);
        velY0 = new NativeArray<float>(dimensions * dimensions, Allocator.Persistent);

        FillArrayJob fillArrayJob = new FillArrayJob();
        fillArrayJob.densityArray = density;
        fillArrayJob.resolution = dimensions;
        // Schedule
        JobHandle jobHandle = fillArrayJob.Schedule(density.Length, 1);

        // Complete and cleanup
        jobHandle.Complete();
      

        for (int i = 0; i < density.Length; i++)
        {
            density[i] = density[i];
            density0[i] = density[i];
        }
    }

    private void OnDestroy()
    {
        density.Dispose();
        density0.Dispose();
        velX.Dispose(); 
        velX0.Dispose();
        velY.Dispose();
        velY0.Dispose();
    }


    // Update is called once per frame
    void Update()
    {
        #region MOUSE INTERACTION        
        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = -Camera.main.transform.position.z; //distance of the plane from the camera
        mousePos = Camera.main.ScreenToWorldPoint(screenPoint);

        Vector3 newMouseVel = (mousePos - prevMousePos) / Time.deltaTime;
        newMouseVel *= mouseVelScalar;
        mouseVel = Vector3.Lerp(mouseVel, newMouseVel, Time.deltaTime * 8);
        prevMousePos = mousePos;
        #endregion


        if (Input.GetMouseButton(0))
        {
            int interactionSize = 4;
            Vector2 coord = GetGridCoord(mousePos);
            coord.x = Math.Clamp(coord.x, 0, dimensions - 1 - interactionSize);
            coord.y = Math.Clamp(coord.y, 0, dimensions - 1 - interactionSize);
            for (int x = 0; x < interactionSize; x++)
            {
                for (int y = 0; y < interactionSize; y++)
                {
                    int index = Index((int)coord.x + x, (int)coord.y + y);
                    velX[index] = mouseVel.x;
                    velY[index] = mouseVel.y;
                    density[index] = 1;
                }
            }
        }

        //VelocityUpdate();
        DensityUpdate();

        //for (int x = 1; x < dimensions - 1; x++)
        //{
        //    for (int y = 1; y < dimensions - 1; y++)
        //    {
        //        int index = Index(x, y);
        //        density[index] -= densityDissipationRate * Time.deltaTime;
        //        density[index] = Mathf.Max(density[index], 0);
        //    }
        //}

        if (Input.GetKey(KeyCode.R))
            Reset();
    }


    // JOBS
    //
    struct FillArrayJob : IJobParallelFor
    {
        public NativeArray<float> densityArray;
        public float resolution;

        public void Execute(int index)
        {
            float fillAmount = (index % resolution) / resolution;
            densityArray[index] = fillAmount;
        }
    }


    void DensityUpdate()
    {
        Swap(density, density0);
        diffuse(dimensions, boundaryMode: 0, density, density0, densityDiffusionRate, Time.fixedDeltaTime);
       
        Swap(density, density0);
        advect(dimensions, boundaryMode: 0, density, density0, velX, velY, Time.fixedDeltaTime);      
    }

    void VelocityUpdate()
    {
        Swap(velX0, velX);
        diffuse(dimensions, 1, velX, velX0, viscosity, Time.fixedDeltaTime);
        Swap(velY0, velY);
        diffuse(dimensions, 2, velY, velY0, viscosity, Time.fixedDeltaTime);
        project(dimensions, velX, velY, velX0, velY0);
        Swap(velX0, velX);
        Swap(velY0, velY);
        advect(dimensions, boundaryMode: 1, velX, velX0, velX0, velY0, Time.fixedDeltaTime);
        advect(dimensions, boundaryMode: 2, velY, velY0, velX0, velY0, Time.fixedDeltaTime);
        project(dimensions, velX, velY, velX0, velY0);
    }

    void diffuse(int dimensions, int boundaryMode, NativeArray<float> newData, NativeArray<float> currentData, float diff, float dt)
    {       
        NativeArray<float> newDataRO = new NativeArray<float>(newData, Allocator.TempJob);
        
        DiffuseJob diffuseJobDensity = new DiffuseJob();
        diffuseJobDensity.dimensions = dimensions;
        diffuseJobDensity.boundaryMode = 0;
        diffuseJobDensity.newData = newData;
        diffuseJobDensity.newDataReadOnly = newDataRO;
        diffuseJobDensity.currentData = currentData;
        diffuseJobDensity.diff = diff;
        diffuseJobDensity.dt = dt;
        diffuseJobDensity.a = dt * diff * dimensions * dimensions;
        JobHandle jobHandle = diffuseJobDensity.Schedule(density.Length, 1);

        for (int iterations = 1; iterations < 20; iterations++)
        {
            diffuseJobDensity = new DiffuseJob();
            diffuseJobDensity.dimensions = dimensions;
            diffuseJobDensity.boundaryMode = 0;
            diffuseJobDensity.newData = newData;
            diffuseJobDensity.newDataReadOnly = newDataRO;
            diffuseJobDensity.currentData = currentData;
            diffuseJobDensity.diff = diff;
            diffuseJobDensity.dt = dt;
            diffuseJobDensity.a = dt * diff * dimensions * dimensions;

            // Schedule
            jobHandle = diffuseJobDensity.Schedule(density.Length, 1, jobHandle);

            // Complete and cleanup
            //jobHandle.Complete();           
            //newData.CopyFrom(diffuseJobDensity.newData);  
        }

        jobHandle.Complete();
        newDataRO.Dispose();
        set_bnd(dimensions, boundaryMode, newData);
        //Swap(toArray, density);
    }

    struct DiffuseJob : IJobParallelFor        
    {
        public int dimensions;
        public int boundaryMode;
        [ReadOnly]public NativeArray<float> newDataReadOnly;
        public NativeArray<float> newData;
        [ReadOnly] public NativeArray<float> currentData;
        public float diff;
        public float dt;
        public float a;

        public void Execute(int index)
        {
            int x = index % dimensions;
            int y = (int)math.floor(index / dimensions);

            if (x == 0 || x == dimensions - 1 || y == 0 || y == dimensions - 1)
            {
                // TODO should just be return
                newData[index] = 1;
                return;
            }
            else
            {
                float neighborSUM = newDataReadOnly[index - 1] + newDataReadOnly[index + 1] + newDataReadOnly[index - dimensions] + newDataReadOnly[index + dimensions];
                newData[index] = (currentData[index] + a * neighborSUM) / (1 + 4 * a);
            }
        }
    }


    void advect(int dimensions, int boundaryMode, NativeArray<float> density, NativeArray<float> density0, NativeArray<float> velX, NativeArray<float> velY, float dt)
    {
        AdvectJob advectJob = new AdvectJob()
        {
            dimensions = dimensions,
            boundaryMode = boundaryMode,
            density = density,
            density0 = density0,
            velX = velX,
            velY = velY,
            dt = dt
        };
    
        JobHandle jobHandle = advectJob.Schedule(density.Length, 1);
        jobHandle.Complete();
        set_bnd(dimensions, boundaryMode, density);
    }

    struct AdvectJob : IJobParallelFor
    {
        public int dimensions;
        public int boundaryMode;
        public NativeArray<float> density;
        [ReadOnly] public NativeArray<float> density0;
        [ReadOnly] public NativeArray<float> velX;
        [ReadOnly] public NativeArray<float> velY;
        public float dt;

        public void Execute(int index)
        {
            float dt0 = dt * dimensions;
            int x = index % dimensions;
            int y = (int)math.floor(index / dimensions);
            float xPos = x - velX[index] * dt0;
            float yPos = y - velY[index] * dt0;

            // Get index by flooring and clamping pos
            int xIndex = (int)math.floor(xPos);
            xIndex = math.clamp(xIndex, 0, dimensions - 2);
            int yIndex = (int)math.floor(yPos);
            yIndex = math.clamp(yIndex, 0, dimensions - 2);

            // Get interpolation amount by getting remainder
            float xInterpolation = xPos - xIndex;
            float yInterpolation = yPos - yIndex;

            float lerpedUpperDensity = math.lerp(density0[Index(xIndex, yIndex)], density0[Index(xIndex + 1, yIndex)], xInterpolation);
            float lerpedLowerDensity = math.lerp(density0[Index(xIndex, yIndex + 1)], density0[Index(xIndex + 1, yIndex + 1)], xInterpolation);
            density[index] = math.lerp(lerpedUpperDensity, lerpedLowerDensity, yInterpolation);        
        }
    }

    void project(int dimensions, NativeArray<float> velX, NativeArray<float> velY, NativeArray<float> p, NativeArray<float> div)
    {
        int k;
        float h = 1.0f / dimensions;
        for (int x = 1; x < dimensions-1; x++)
        {
            for (int y = 1; y < dimensions-1; y++)
            {
                div[Index(x,y)] = -0.5f * h * (velX[Index(x + 1, y)] - velX[Index(x - 1, y)] +
                                                velY[Index(x,y + 1)] - velY[Index(x,y - 1)]);
                p[Index(x,y)] = 0;
            }
        }

        set_bnd(dimensions, 0, div); 
        set_bnd(dimensions, 0, p);

        for (k = 0; k < 20; k++)
        {
            for (int x = 1; x < dimensions - 1; x++)
            {
                for (int y = 1; y < dimensions - 1; y++)
                {
                    p[Index(x,y)] = (div[Index(x,y)] + p[Index(x - 1, y)] + p[Index(x + 1, y)] +
                     p[Index(x,y - 1)] + p[Index(x,y + 1)]) / 4;
                }
            }
            set_bnd(dimensions, 0, p);
        }
        for (int x = 1; x < dimensions - 1; x++)
        {
            for (int y = 1; y < dimensions - 1; y++)
            {
                velX[Index(x,y)] -= 0.5f * (p[Index(x + 1, y)] - p[Index(x - 1, y)]) / h;
                velY[Index(x,y)] -= 0.5f * (p[Index(x,y + 1)] - p[Index(x,y - 1)]) / h;
            }
        }
        set_bnd(dimensions, 1, velX);
        set_bnd(dimensions, 2, velY);
    }

    static void set_bnd(int dimensions, int boundaryMode, NativeArray<float> inputArray )
    {    
        // SET BORDERS
        for (int i = 1; i < dimensions-1; i++)
        {
            inputArray[Index(0,i)] = boundaryMode == 1 ? -inputArray[Index(1, i)] : inputArray[Index(1, i)];
            inputArray[Index(dimensions - 1, i)] = boundaryMode == 1 ? -inputArray[Index(dimensions - 2, i)] : inputArray[Index(dimensions - 2, i)];
            inputArray[Index(i,0)] = boundaryMode == 2 ? -inputArray[Index(i,1)] : inputArray[Index(i,1)];
            inputArray[Index(i, dimensions - 1)] = boundaryMode == 2 ? -inputArray[Index(i, dimensions - 2)] : inputArray[Index(i, dimensions - 2)];
        }

        // SET CORNERS
        inputArray[Index(0, 0)] = 0.5f * (inputArray[Index(1, 0)] + inputArray[Index(0, 1)]);
        inputArray[Index(0, dimensions-1)] = 0.5f * (inputArray[Index(1, dimensions-1)] + inputArray[Index(0, dimensions-2)]);
        inputArray[Index(dimensions-1, 0)] = 0.5f * (inputArray[Index(dimensions -2, 0)] + inputArray[Index(dimensions -1, 1)]);
        inputArray[Index(dimensions -1, dimensions-1)] = 0.5f * (inputArray[Index(dimensions -2, dimensions-1)] + inputArray[Index(dimensions -1, dimensions-2)]);
    }

    void Reset()
    {
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {              
                density[Index(x,y)] = 0;
                density0[Index(x,y)] = 0;
                velX[Index(x,y)] = 0;
                velY[Index(x,y)] = 0;
            }
        }
    }

    #region HELPER AND GIZMOS
    void Swap(NativeArray<float> fromArray, NativeArray<float> tooArray)
    {
        for (int i = 0; i < fromArray.Length; i++)
        {
            float temp = fromArray[i];
            fromArray[i] = tooArray[i];
            tooArray[i] = temp;
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
        if (!Application.isPlaying)
            return;

        float size = 10f;
        float cellSize = size / (float)dimensions;
        Vector3 startPos = new Vector3(-size * .5f, -size * .5f, 0);
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                Vector3 pos = startPos + new Vector3(x * cellSize, y * cellSize, 0);

                // DENISTY
                Gizmos.color = Color.white * density[Index(x,y)];
                Gizmos.DrawCube(pos, Vector3.one * cellSize);

                //// DEBUG DENSITY DIFFUSE DIFF
                //Gizmos.color = Color.white * Mathf.Clamp01(debugDensityDiffuseDiff[index(x,y)]);
                //Gizmos.DrawCube(pos + Vector3.right * size, Vector3.one * cellSize);

                //// DEBUG DENSITY ADVECT DIFF
                //Gizmos.color = Color.white * Mathf.Clamp01(debugDensityAdvectionDiff[index(x,y)]);
                //Gizmos.DrawCube(pos + Vector3.right * size * 2f, Vector3.one * cellSize);

                // VECTORFIELD
                Gizmos.color = Color.white * .2f;
                Vector3 vel = new Vector3
                (
                    velX[Index(x,y)] * cellSize,
                    velY[Index(x,y)] * cellSize,
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
    #endregion
}
