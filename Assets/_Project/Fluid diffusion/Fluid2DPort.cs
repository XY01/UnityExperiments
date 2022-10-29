using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public class Fluid2DPort : MonoBehaviour
{
    public const int dimensions = 32;

    float[,] density = new float[dimensions, dimensions];
    float[,] density0 = new float[dimensions, dimensions];

    Vector2[,] velocities = new Vector2[dimensions, dimensions];

  
    Vector2[,] velocities0 = new Vector2[dimensions, dimensions];


    public float densityDiffusionRate = 1;
    public float advectionRate = 1;

    Vector3 mouseVel;
    Vector3 prevMousePos;
    public Vector3 mousePos;

    public float debugDensityAdd = 1f;



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
                    density0[x, y] = 0;
                }
                else
                {
                    density[x, y] = xNorm * yNorm;
                    density0[x, y] = xNorm * yNorm;
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
                density0[x, y] = 0;
                velocities[x, y] = Vector2.zero;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.D))
        {
            //Swap(density, density0);
            Diffuse(density0, density, densityDiffusionRate, Time.fixedDeltaTime);
            Swap(density0, density);
            Advection(Time.fixedDeltaTime * advectionRate);
            Swap(density0, density);
            //DiffuseNew(density, density0, densityDiffusionRate, Time.fixedDeltaTime);
        }

        if (Input.GetKey(KeyCode.A))
           

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
            density0[(int)coord.x, (int)coord.y] += Time.deltaTime * 10;
        }
    }



    #region PORTED FUNCTIONS   
    void Diffuse(float[,] toArray, float[,] fromArray, float diff, float dt)
    {
        float a = dt * diff * dimensions * dimensions;
        for (int iterations = 0; iterations < 20; iterations++)
        {
            for (int i = 1; i < dimensions-1; i++)
            {
                for (int j = 1; j < dimensions-1; j++)
                {
                    float neighborAggregate = toArray[i - 1, j] + toArray[i + 1, j] + toArray[i, j - 1] + toArray[i, j + 1];
                    toArray[i, j] = (fromArray[i, j] + a * neighborAggregate) / (1 + 4 * a);
                }
            }
            SetBoundaries(true, true, toArray, dimensions);
        }
    }

    void Advection(float timeStep)
    {
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                // Position backwards along vel vector
                float xPos = x - velocities[x, y].x * timeStep;
                float yPos = y - velocities[x, y].y * timeStep;

                // Get index by flooring and clamping pos
                int xIndex = Mathf.FloorToInt(xPos);
                xIndex = Mathf.Clamp(xIndex, 0, dimensions - 2);
                int yIndex = Mathf.FloorToInt(yPos);
                yIndex = Mathf.Clamp(yIndex, 0, dimensions - 2);

                // Get interpolation amount by getting remainder
                float xInterpolation = xPos - xIndex;
                float yInterpolation = yPos - yIndex;

                float lerpedUpperDensity = Mathf.Lerp(density[xIndex, yIndex], density[xIndex + 1, yIndex], xInterpolation);
                float lerpedLowerDensity = Mathf.Lerp(density[xIndex, yIndex + 1], density[xIndex + 1, yIndex + 1], xInterpolation);
                density0[x, y] = Mathf.Lerp(lerpedUpperDensity, lerpedLowerDensity, yInterpolation);

                // VELOCITY
                //Vector2 lerpedUpperVel = Vector2.Lerp(velocities[xIndex, yIndex], velocities[xIndex + 1, yIndex], xInterpolation);
                //Vector2 lerpedLowerVel = Vector2.Lerp(velocities[xIndex, yIndex + 1], velocities[xIndex + 1, yIndex + 1], xInterpolation);
                //targetVelocities[x, y] = Vector2.Lerp(lerpedUpperVel, lerpedLowerVel, yInterpolation);
                //targetVelocities[x, y] = velocities[x, y];
            }
        }
        SetBoundaries(true, true, density0, dimensions);
        //InterpolateDensityBuffers(timeStep);
    }

    //void advect(float[,] d, float[,] d0, Vector2[,] vel, float dt)
    //{
    //    int i0, j0, i1, j1;
    //    float x, y, s0, t0, s1, t1, dt0;
    //    dt0 = dt * N;
    //    for (int i = 1; i <= N; i++)
    //    {
    //        for (int j = 1; j <= N; j++)
    //        {
    //            float x = i - dt0 * vel[i, j].x;
    //            float y = j - dt0 * vel[i, j].y;

    //            if (x < 0.5) x = 0.5; 
    //            if (x > N + 0.5) x = N + 0.5; 
    //            float i0 = (int)x; 
    //            float i1 = i0 + 1;

    //            if (y < 0.5) y = 0.5;
    //            if (y > N + 0.5) y = N + 0.5;
    //            float j0 = (int)y; 
    //            float j1 = j0 + 1;

    //            s1 = x - i0; 
    //            s0 = 1 - s1; 
    //            t1 = y - j0; 
    //            t0 = 1 - t1;

    //            d[i, j] = s0 * (t0 * d0[i0, j0] + t1 * d 0[i0, j1])+
    //                      s1 * (t0 * d0[i1, j0] + t1 * d0[i1, j1]);
    //        }
    //    }
    //    SetBoundaries(true, true, d, dimensions);
    //}


    /*
    void advect(int N, int b, float* d, float* d0, float* u, float* v, float dt)
    {
        int i, j, i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;
        dt0 = dt * N;
        for (i = 1; i <= N; i++)
        {
            for (j = 1; j <= N; j++)
            {
                x = i - dt0 * u[IX(i, j)]; y = j - dt0 * v[IX(i, j)];
                if (x < 0.5) x = 0.5; if (x > N + 0.5) x = N + 0.5; i0 = (int)x; i1 = i0 + 1;
                if (y < 0.5) y = 0.5; if (y > N + 0.5) y = N + 0.5; j0 = (int)y; j1 = j0 + 1;
                s1 = x - i0; s0 = 1 - s1; t1 = y - j0; t0 = 1 - t1;
                d[IX(i, j)] = s0 * (t0 * d0[IX(i0, j0)] + t1 * d 0[IX(i0, j1)])+
                 s1 * (t0 * d0[IX(i1, j0)] + t1 * d0[IX(i1, j1)]);
            }
        }
        set_bnd(N, b, d);
    }
*/

    public void SetBoundaries(bool xBoundaries, bool yBoundaries, float[,] x, int dimensions)
    {
        // SET BOUNDARY TO SAME OR NEGATIVE OF NEIGHBOR
        //
        for (int i = 1; i < dimensions-1; i++)
        {
            x[0, i] = yBoundaries ? -x[1, i] : x[1, i];
            x[dimensions - 1, i] = yBoundaries ? -x[dimensions - 1, i] : x[dimensions - 1, i];

            x[i, 0] = xBoundaries ? -x[i, 1] : x[i, 1];
            x[i, dimensions - 1] = xBoundaries ? -x[i, dimensions - 1] : x[i, dimensions - 1];
        }

        // SET CORNERS
        //
        x[0, 0] = 0.5f * (x[1, 0] + x[0, 1]);
        x[0, dimensions - 1] = 0.5f * (x[1, dimensions - 1] + x[0, dimensions - 2]);
        x[dimensions - 1, 0] = 0.5f * (x[dimensions - 2, 0] + x[dimensions - 1, 1]);
        x[dimensions - 1, dimensions - 1] = 0.5f * (x[dimensions - 2, dimensions - 1] + x[dimensions - 1, dimensions - 2]);
    }


    void Swap(float[,] fromArray, float[,]tooArray)
    {
        for (int i = 1; i < dimensions - 1; i++)
        {
            for (int j = 1; j < dimensions - 1; j++)
            {
                float temp = fromArray[i,j];
                fromArray[i, j] = tooArray[i, j];
                tooArray[i, j] = temp;
            }
        }
    }

    /*
     * void set_bnd ( int N, int b, flo at * x )
    {
    int i;
        for ( i=1 ; i<=N ; i++ )
        {
            x[IX(0 ,i)] = b==1 ? –x[IX(1,i)] : x[IX(1,i)];
            x[IX(N+1,i)] = b==1 ? –x[IX(N,i)] : x[IX(N,i)];
            x[IX(i,0 )] = b==2 ? –x[IX(i,1)] : x[IX(i,1)];
            x[IX(i,N+1)] = b==2 ? –x[IX(i,N)] : x[IX(i,N)];
        }

        x[IX(0 ,0 )] = 0.5*(x[IX(1,0 )]+x[IX(0 ,1)]);
        x[IX(0 ,N+1)] = 0.5*(x[IX(1,N+1)]+x[IX(0 ,N )]);
        x[IX(N+1,0 )] = 0.5*(x[IX(N,0 )]+x[IX(N+1,1)]);
        x[IX(N+1,N+1)] = 0.5*(x[IX(N,N+1)]+x[IX(N+1,N )]);
    }

    void diffuse(int N, int b, float* x, float* x0, float diff, float dt)
    {
        int i, j, k;
        float a = dt * diff * N * N;
        for (k = 0; k < 20; k++)
        {
            for (i = 1; i <= N; i++)
            {
                for (j = 1; j <= N; j++)
                {
                    x[IX(i, j)] = (x0[IX(i, j)] + a * (x[IX(i - 1, j)] + x[IX(i + 1, j)] +
                     x[IX(i, j - 1)] + x[IX(i, j + 1)])) / (1 + 4 * a);
                }
            }
            set_bnd(N, b, x);
        }
    }
    */

    void DiffuseNew(float[,] x, float[,] x0, float diffusionStrength, float dt)
    {
        float a = dt * diffusionStrength;
        linearSolver(density, density0, a, dt, 1);        
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

    #endregion

    #region HELPER AND GIZMOS
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
    #endregion
}
