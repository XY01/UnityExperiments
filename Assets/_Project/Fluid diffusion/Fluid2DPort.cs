using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;


// REFS
// https://www.dgp.toronto.edu/public_user/stam/reality/Research/pdf/GDC03.pdf
// https://www.youtube.com/watch?v=alhpH6ECFvQ
// https://www.youtube.com/watch?v=qsYE1wMEMPA
// https://www.youtube.com/watch?v=rB83DpBJQsE&t=817s

// TODO 
// - Walls
// - Height map
public class Fluid2DPort : MonoBehaviour
{
    #region VARIABLES    
    public const int dimensions = 64;
    public float densityDiffusionRate = .0001f;
    public float viscosity = 1;
    public float densityDissipationRate = .5f;

    public float mouseVelScalar = 1;

    float[,] density = new float[dimensions, dimensions];
    float[,] density0 = new float[dimensions, dimensions];

    float[,] velX = new float[dimensions, dimensions];
    float[,] velX0 = new float[dimensions, dimensions];

    float[,] velY = new float[dimensions, dimensions];
    float[,] velY0 = new float[dimensions, dimensions];


    // MOUSE INPUT
    Vector3 mouseVel;
    Vector3 prevMousePos;
    Vector3 mousePos;

    [Header("DEBUG")]
    public float debugDiffScalar = 1;
    float[,] debugDensityDiffuseDiff = new float[dimensions, dimensions];
    float[,] debugDensityAdvectionDiff = new float[dimensions, dimensions];
    #endregion


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
            }
        }
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
                    velX[(int)coord.x + x, (int)coord.y + y] = mouseVel.x;
                    velY[(int)coord.x + x, (int)coord.y + y] = mouseVel.y;
                    density[(int)coord.x + x, (int)coord.y + y] = 1;
                }
            }
        }

        VelocityUpdate();
        DensityUpdate();

        for (int i = 1; i < dimensions - 1; i++)
        {
            for (int j = 1; j < dimensions - 1; j++)
            {
                density[i, j] -= densityDissipationRate * Time.deltaTime;
                density[i, j] = Mathf.Max(density[i, j], 0);
            }
        }

        if (Input.GetKey(KeyCode.R))
            Reset();
    }

    void DensityUpdate()
    {
        Swap(density0, density);
        diffuse(dimensions, boundaryMode: 0, density, density0, densityDiffusionRate, Time.fixedDeltaTime);
        Swap(density0, density);
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

    void diffuse(int dimensions, int boundaryMode, float[,] toArray, float[,] fromArray, float diff, float dt)
    {
        float a = dt * diff * dimensions * dimensions;
        for (int iterations = 0; iterations < 20; iterations++)
        {
            for (int i = 1; i < dimensions-1; i++)
            {
                for (int j = 1; j < dimensions-1; j++)
                {
                    float neighborSUM = toArray[i - 1, j] + toArray[i + 1, j] + toArray[i, j - 1] + toArray[i, j + 1];
                    toArray[i, j] = (fromArray[i, j] + a * neighborSUM) / (1 + 4 * a);
                }
            }
            set_bnd(dimensions, boundaryMode, toArray);
        }
    }

    void advect(int dimensions, int boundaryMode, float[,] density, float[,] density0, float[,] velX, float[,] velY, float dt)
    {       
        float dt0 = dt * dimensions;
        for (int x = 1; x < dimensions-1; x++)
        {
            for (int y = 1; y < dimensions-1; y++)
            {
                float xPos = x - velX[x, y] * dt0;
                float yPos = y - velY[x, y] * dt0;

                // Get index by flooring and clamping pos
                int xIndex = Mathf.FloorToInt(xPos);
                xIndex = Mathf.Clamp(xIndex, 0, dimensions - 2);
                int yIndex = Mathf.FloorToInt(yPos);
                yIndex = Mathf.Clamp(yIndex, 0, dimensions - 2);

                // Get interpolation amount by getting remainder
                float xInterpolation = xPos - xIndex;
                float yInterpolation = yPos - yIndex;

                float lerpedUpperDensity = Mathf.Lerp(density0[xIndex, yIndex], density0[xIndex + 1, yIndex], xInterpolation);
                float lerpedLowerDensity = Mathf.Lerp(density0[xIndex, yIndex + 1], density0[xIndex + 1, yIndex + 1], xInterpolation);
                density[x, y] = Mathf.Lerp(lerpedUpperDensity, lerpedLowerDensity, yInterpolation);
            }
        }

        set_bnd(dimensions, boundaryMode, density);
    }

    void project(int dimensions, float[,] velX, float[,] velY, float[,] p, float[,] div)
    {
        int k;
        float h = 1.0f / dimensions;
        for (int x = 1; x < dimensions-1; x++)
        {
            for (int y = 1; y < dimensions-1; y++)
            {
                div[x, y] = -0.5f * h * (velX[x + 1, y] - velX[x - 1, y] +
                velY[x, y + 1] - velY[x, y - 1]);
                p[x, y] = 0;
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
                    p[x, y] = (div[x, y] + p[x - 1, y] + p[x + 1, y] +
                     p[x, y - 1] + p[x, y + 1]) / 4;
                }
            }
            set_bnd(dimensions, 0, p);
        }
        for (int x = 1; x < dimensions - 1; x++)
        {
            for (int y = 1; y < dimensions - 1; y++)
            {
                velX[x, y] -= 0.5f * (p[x + 1, y] - p[x - 1, y]) / h;
                velY[x, y] -= 0.5f * (p[x, y + 1] - p[x, y - 1]) / h;
            }
        }
        set_bnd(dimensions, 1, velX);
        set_bnd(dimensions, 2, velY);
    }

    void set_bnd(int dimensions, int boundaryMode, float[,] inputArray )
    {    
        // SET BORDERS
        for (int i = 1; i < dimensions-1; i++)
        {
            inputArray[0,i] = boundaryMode == 1 ? -inputArray[1,i] : inputArray[1,i];
            inputArray[dimensions-1, i] = boundaryMode == 1 ? -inputArray[dimensions-2, i] : inputArray[dimensions-2, i];
            inputArray[i,0] = boundaryMode == 2 ? -inputArray[i,1] : inputArray[i,1];
            inputArray[i, dimensions - 1] = boundaryMode == 2 ? -inputArray[i, dimensions - 2] : inputArray[i, dimensions - 2];
        }

        // SET CORNERS
        inputArray[0, 0] = 0.5f * (inputArray[1, 0] + inputArray[0, 1]);
        inputArray[0, dimensions-1] = 0.5f * (inputArray[1, dimensions-1] + inputArray[0, dimensions-2]);
        inputArray[dimensions-1, 0] = 0.5f * (inputArray[dimensions-2, 0] + inputArray[dimensions-1, 1]);
        inputArray[dimensions-1, dimensions-1] = 0.5f * (inputArray[dimensions-2, dimensions-1] + inputArray[dimensions-1, dimensions-2]);
    }

    void Reset()
    {
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                density[x, y] = 0;
                density0[x, y] = 0;
                velX[x, y] = 0;
                velY[x, y] = 0;
            }
        }
    }

    #region HELPER AND GIZMOS
    void Swap(float[,] fromArray, float[,] tooArray)
    {
        for (int i = 1; i < dimensions - 1; i++)
        {
            for (int j = 1; j < dimensions - 1; j++)
            {
                float temp = fromArray[i, j];
                fromArray[i, j] = tooArray[i, j];
                tooArray[i, j] = temp;
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
                Gizmos.color = Color.white * density[x, y];
                Gizmos.DrawCube(pos, Vector3.one * cellSize);

                // DEBUG DENSITY DIFFUSE DIFF
                Gizmos.color = Color.white * Mathf.Clamp01(debugDensityDiffuseDiff[x, y]);
                Gizmos.DrawCube(pos + Vector3.right * size, Vector3.one * cellSize);

                // DEBUG DENSITY ADVECT DIFF
                Gizmos.color = Color.white * Mathf.Clamp01(debugDensityAdvectionDiff[x, y]);
                Gizmos.DrawCube(pos + Vector3.right * size * 2f, Vector3.one * cellSize);

                // VECTORFIELD
                Gizmos.color = Color.white * .2f;
                Vector3 vel = new Vector3
                (
                    velX[x, y] * cellSize,
                    velY[x, y] * cellSize,
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
