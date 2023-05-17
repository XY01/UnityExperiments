using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SmokeSimulation : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public int depth = 10;
    public Voxel[,,] voxels;

    public float diffusionRate = 0.2f;
    public float buoyancyForce = 0.1f;
    public float minSmokeAmountToDiffuse = 0.5f;


    public float AddSmokeRate = 10;

    void Start()
    {
        InitializeVoxels();
    }

    void Update()
    {
        SimulateSmokeDiffusion();

        if (Input.GetKey(KeyCode.A))
            AddSmoke();
    }

    void InitializeVoxels()
    {
        voxels = new Voxel[width, height, depth];

        // Initialize voxels
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    voxels[x, y, z] = new Voxel();
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1 || z == 0 || z == depth - 1)
                    {
                        voxels[x, y, z].IsSolid = true;
                    }
                }
            }
        }
    }


    private void AddSmoke()
    {
        voxels[5, 2, 5].smokeAmount += AddSmokeRate * Time.deltaTime;
        voxels[5, 2, 6].smokeAmount += AddSmokeRate * Time.deltaTime;
        voxels[6, 2, 6].smokeAmount += AddSmokeRate * Time.deltaTime;
        voxels[6, 2, 5].smokeAmount += AddSmokeRate * Time.deltaTime;
    }


    void SimulateSmokeDiffusion()
    {
        Voxel[,,] nextVoxels = (Voxel[,,])voxels.Clone();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    if (!voxels[x, y, z].IsSolid && voxels[x, y, z].smokeAmount >= minSmokeAmountToDiffuse)
                    {
                        float diffusedSmoke = 0.0f;
                        int surroundingVoxels = 0;

                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                for (int k = -1; k <= 1; k++)
                                {
                                    if (!voxels[x + i, y + j, z + k].IsSolid && (i == 0 || j == 0 || k == 0) && (i != 0 && j != 0 && k != 0))
                                    {
                                        diffusedSmoke += voxels[x + i, y + j, z + k].smokeAmount;
                                        surroundingVoxels++;
                                    }
                                }
                            }
                        }

                        float avgSmoke = diffusedSmoke / surroundingVoxels;
                        float deltaSmoke = (avgSmoke - voxels[x, y, z].smokeAmount) * diffusionRate;

                        nextVoxels[x, y, z].smokeAmount -= deltaSmoke;

                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                for (int k = -1; k <= 1; k++)
                                {
                                    if (!voxels[x + i, y + j, z + k].IsSolid && (i == 0 || j == 0 || k == 0) && (i != 0 && j != 0 && k != 0))
                                    {
                                        nextVoxels[x + i, y + j, z + k].smokeAmount += deltaSmoke / surroundingVoxels;
                                    }
                                }
                            }
                        }

                        // Apply buoyancy force
                        if (y < height - 1 && !voxels[x, y + 1, z].IsSolid)
                        {
                            float buoyancy = Mathf.Max(0, voxels[x, y, z].smokeAmount - voxels[x, y + 1, z].smokeAmount) * buoyancyForce;
                            nextVoxels[x, y + 1, z].smokeAmount += buoyancy;
                            nextVoxels[x, y, z].smokeAmount -= buoyancy;
                        }

                        nextVoxels[x, y, z].smokeAmount = Mathf.Clamp01(nextVoxels[x, y, z].smokeAmount);
                    }
                }
            }
        }

        voxels = nextVoxels;
    }



    //void SimulateSmokeDiffusion()
    //{
    //    Voxel[,,] nextVoxels = (Voxel[,,])voxels.Clone();

    //    for (int x = 1; x < width - 1; x++)
    //    {
    //        for (int y = 1; y < height - 1; y++)
    //        {
    //            for (int z = 1; z < depth - 1; z++)
    //            {
    //                if (!voxels[x, y, z].IsSolid)
    //                {
    //                    float diffusedSmoke = 0.0f;
    //                    int surroundingVoxels = 0;

    //                    for (int i = -1; i <= 1; i++)
    //                    {
    //                        for (int j = -1; j <= 1; j++)
    //                        {
    //                            for (int k = -1; k <= 1; k++)
    //                            {
    //                                if (!voxels[x + i, y + j, z + k].IsSolid && (i == 0 || j == 0 || k == 0))
    //                                {
    //                                    diffusedSmoke += voxels[x + i, y + j, z + k].smokeAmount;
    //                                    surroundingVoxels++;
    //                                }
    //                            }
    //                        }
    //                    }

    //                    nextVoxels[x, y, z].smokeAmount = Mathf.Lerp(voxels[x, y, z].smokeAmount, diffusedSmoke / surroundingVoxels, diffusionRate * Time.deltaTime);

    //                    // Apply buoyancy force
    //                    if (y < height - 1 && !voxels[x, y + 1, z].IsSolid)
    //                    {
    //                        float buoyancy = Mathf.Max(0, voxels[x, y, z].smokeAmount - voxels[x, y + 1, z].smokeAmount) * buoyancyForce * Time.deltaTime;
    //                        nextVoxels[x, y + 1, z].smokeAmount += buoyancy;
    //                        nextVoxels[x, y, z].smokeAmount -= buoyancy;
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    voxels = nextVoxels;
    //}

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // draw gizmos showing all voxels smoke amount and isSolid        if (voxels != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        if (voxels[x, y, z].IsSolid)
                        {
                            Gizmos.color = new Color(1, 1, 1, .03f);//, voxels[x, y, z].smokeAmount / .3f);
                            Gizmos.DrawWireCube(new Vector3(x, y, z), Vector3.one);
                        }
                        else
                        {
                            Gizmos.color = new Color(0, 0, 0, voxels[x, y, z].smokeAmount / .3f);
                            Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one);
                        }
                       
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class Voxel
{
    public bool IsSolid;
    public float smokeAmount;
}
