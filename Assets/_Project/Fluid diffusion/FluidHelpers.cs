using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidHelpers
{
    public static float[,] ScalarFieldFromNeighbors(float[,] array)
    {
        int xDimensions = array.GetUpperBound(0) + 1;
        int yDimensions = array.GetUpperBound(1) + 1;
        float[,] outputArray = new float[xDimensions, yDimensions];

        for (int x = 0; x < xDimensions; x++)
        {
            for (int y = 0; y < yDimensions; y++)
            {
                float currentValue = array[x, y];
                float left = currentValue;
                float right = currentValue;
                float up = currentValue;
                float down = currentValue;

                if (x > 0)
                {
                    left = array[x - 1, y];
                }
                if (x < (xDimensions - 1))
                {
                    right = array[x + 1, y];
                }
                if (y > 0)
                {
                    down = array[x, y - 1];
                }
                if (y < (yDimensions - 1))
                {
                    up = array[x, y + 1];
                }

                outputArray[x,y] = (left + right + up + down) / 4f;
            }
        }

        return outputArray;
    }

    public static Vector2[,] VectorfieldFromNeighbors(Vector2[,] array)
    {
        int xDimensions = array.GetUpperBound(0) + 1;
        int yDimensions = array.GetUpperBound(1) + 1;
        Vector2[,] outputArray = new Vector2[xDimensions, yDimensions];

        for (int x = 0; x < xDimensions; x++)
        {
            for (int y = 0; y < yDimensions; y++)
            {
                Vector2 currentValue = array[x, y];
                Vector2 left = currentValue;
                Vector2 right = currentValue;
                Vector2 up = currentValue;
                Vector2 down = currentValue;

                if (x > 0)
                {
                    left = array[x - 1, y];
                }
                if (x < (xDimensions - 1))
                {
                    right = array[x + 1, y];
                }
                if (y > 0)
                {
                    down = array[x, y - 1];
                }
                if (y < (yDimensions - 1))
                {
                    up = array[x, y + 1];
                }

                outputArray[x, y] = (left + right + up + down) / 4f;
            }
        }

        return outputArray;
    }

    // Function of solving linear differential equation
    public void lin_solve(int b, float[,]array, float[,]arrayPrev, float a, float c, int iterations)
    {
        float cRecip = 1f / c;
        int xDimensions = array.GetUpperBound(0) + 1;
        int yDimensions = array.GetUpperBound(1) + 1;
        for (int t = 0; t < iterations; t++)
        {
            for (int x = 1; x < xDimensions; x++)
            {
                for (int y = 1; y < yDimensions; y++)
                {
                    float neighborAggregate = array[x + 1,y] +
                                                array[x - 1, y] +
                                                array[x, y + 1] +
                                                array[x, y - 1];

                    array[x,y] = arrayPrev[x,y] + a * neighborAggregate * cRecip;
                }
            }
            //set_bnd(b, x);
        }
    }

    public static void SetBoundaries(bool xBoundaries, bool yBoundaries, float[,] x, int dimensions)
    {
        // SET BOUNDARY TO SAME OR NEGATIVE OF NEIGHBOR
        //
        for (int i = 1; i < dimensions - 1; i++)
        {
            x[i, 0] = xBoundaries ? -x[i, 1] : x[i, 1];
            x[i, dimensions - 1] = xBoundaries ? -x[i, dimensions- 2] : x[i, dimensions- 2];
        }
        for (int j = 1; j < dimensions- 1; j++)
        {
            x[0, j] = yBoundaries ? -x[1, j] : x[1, j];
            x[dimensions- 1, j] = yBoundaries ? -x[dimensions- 2, j] : x[dimensions- 2, j];
        }

        // SET CORNERS
        //
        x[0, 0] = 0.5f * (x[1, 0] + x[0, 1]);
        x[0, dimensions- 1] = 0.5f * (x[1, dimensions- 1] + x[0, dimensions- 2]);
        x[dimensions - 1, 0] = 0.5f * (x[dimensions- 2, 0] + x[dimensions- 1, 1]);
        x[dimensions - 1, dimensions- 1] = 0.5f * (x[dimensions- 2, dimensions- 1] + x[dimensions- 1, dimensions - 2]);
    }
    /*
     * 
     * 
    function lin_solve(b, x, x0, a, c) 
    {
        let cRecip = 1.0 / c;
        for (let t = 0; t < iter; t++)
        {
            for (let j = 1; j < N - 1; j++)
            {
                for (let i = 1; i < N - 1; i++)
                {
                    x[IX(i, j)] = (x0[IX(i, j)] +
                        a *
                          (x[IX(i + 1, j)] +
                            x[IX(i - 1, j)] +
                            x[IX(i, j + 1)] +
                            x[IX(i, j - 1)])) *
                      cRecip;
                }
            }
            set_bnd(b, x);
        }
    }

    function set_bnd(b, x) {
  for (let i = 1; i <  - 1; i++) {
    x[IX(i, 0)] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
    x[IX(i, N - 1)] = b == 2 ? -x[IX(i, N - 2)] : x[IX(i, N - 2)];
  }
  for (let j = 1; j < N - 1; j++) {
    x[IX(0, j)] = b == 1 ? -x[IX(1, j)] : x[IX(1, j)];
    x[IX(N - 1, j)] = b == 1 ? -x[IX(N - 2, j)] : x[IX(N - 2, j)];
  }

  x[IX(0, 0)] = 0.5 * (x[IX(1, 0)] + x[IX(0, 1)]);
  x[IX(0, N - 1)] = 0.5 * (x[IX(1, N - 1)] + x[IX(0, N - 2)]);
  x[IX(N - 1, 0)] = 0.5 * (x[IX(N - 2, 0)] + x[IX(N - 1, 1)]);
  x[IX(N - 1, N - 1)] = 0.5 * (x[IX(N - 2, N - 1)] + x[IX(N - 1, N - 2)]);
}
    */
}
