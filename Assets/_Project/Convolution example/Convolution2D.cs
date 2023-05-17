using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Convolution2D : MonoBehaviour
{
    class Voxel
    {
        public Vector3 position;
        public float value = 0;
        public float newValue = 0;
    }

    public int2 dimensions = new int2( 20, 10 );
    public float voxelSize = .25f;

    public float paintValue = 10;

    float[] diffuseKernel = new float[9]
    {
         .0625f, .125f, .0625f,
        .125f, .25f, .125f,
        .0625f, .125f, .0625f
       
    };


    float[] diffuseKernel1 = new float[9]
    {
        0.03125f, .0625f, 0.03125f,
        .0625f, .625f, .0625f,
        0.03125f, .0625f, 0.03125f
    };

    Voxel[,] voxelGrid;
    Plane plane;

    Vector3 mousePos;

    public float decay = .01f;

    // Start is called before the first frame update
    void Start()
    {
        voxelGrid = new Voxel[dimensions.x, dimensions.y];
        Vector3 offset = new Vector3(-dimensions.x * voxelSize * .5f, 0, -dimensions.y * voxelSize * .5f);
        for (int x = 0; x < dimensions.x; x++)
        {
            for(int y = 0; y < dimensions.y; y++)
            {
                voxelGrid[x, y] = new Voxel();
                voxelGrid[x, y].position = offset + new Vector3(x * voxelSize, 0, y * voxelSize);
            }
        }

        plane = new Plane(Vector3.up, Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        //--- GET MOUSE POS ON PLANE
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float enter = 0.0f;
        if (plane.Raycast(ray, out enter))        
            mousePos = ray.GetPoint(enter);

        //--- ADD VALUE TO VOXEL AT MOUSE POS
        if (Input.GetMouseButton(0))
        {
            foreach (Voxel vox in voxelGrid)
            {
                if (Vector3.Distance(vox.position, mousePos) < voxelSize)
                {
                    vox.value += paintValue * Time.deltaTime;
                    vox.value = Mathf.Clamp01(vox.value);

                    // Setting new value so edge verts that get painted maintain their value
                    vox.newValue = vox.value;
                }
            }
        }

        //--- CLEAR VALUES
        if (Input.GetKeyDown(KeyCode.C))
            Clear();

        //--- CONVOLUTION
        if (Input.GetKey(KeyCode.D))
        {
            for (int x = 1; x < dimensions.x - 1; x++)
            {
                for (int y = 1; y < dimensions.y - 1; y++)
                {
                    float newValue = 0;

                    newValue += voxelGrid[x - 1, y - 1].value * diffuseKernel[0];
                    newValue += voxelGrid[x, y - 1].value * diffuseKernel[1];
                    newValue += voxelGrid[x + 1, y - 1].value * diffuseKernel[2];

                    newValue += voxelGrid[x - 1, y].value * diffuseKernel[3];
                    newValue += voxelGrid[x, y].value * diffuseKernel[4];
                    newValue += voxelGrid[x + 1, y].value * diffuseKernel[5];

                    newValue += voxelGrid[x - 1, y + 1].value * diffuseKernel[6];
                    newValue += voxelGrid[x, y + 1].value * diffuseKernel[7];
                    newValue += voxelGrid[x + 1, y + 1].value * diffuseKernel[8];

                    voxelGrid[x, y].newValue = newValue;
                }
            }

            //--- MOVE NEW VALUES INTO VALUE
            foreach (Voxel vox in voxelGrid)
            {
                vox.newValue -= decay * Time.deltaTime;
            }

            //--- MOVE NEW VALUES INTO VALUE
            foreach (Voxel vox in voxelGrid)
            {
                vox.value = vox.newValue;
                vox.value = Mathf.Max(vox.value, 0);
            }

            for (int y = 0; y < dimensions.y; y++)
            {
                voxelGrid[0, y].value = voxelGrid[1, y].value;
                voxelGrid[0, y].newValue = voxelGrid[1, y].newValue;

                voxelGrid[dimensions.x-1, y].value = voxelGrid[dimensions.x-2, y].value;
                voxelGrid[dimensions.x-1, y].newValue = voxelGrid[dimensions.x-2, y].newValue;
            }

            for (int x = 0; x < dimensions.x; x++)
            {
                voxelGrid[x, 0].value = voxelGrid[x,1].value;
                voxelGrid[x, 0].newValue = voxelGrid[x, 1].newValue;

                voxelGrid[x, dimensions.y - 1].value = voxelGrid[x, dimensions.y - 2].value;
                voxelGrid[x, dimensions.y - 1].newValue = voxelGrid[x, dimensions.y - 2].newValue;
            }
        }
    }

    void Clear()
    {
        foreach (Voxel vox in voxelGrid)
        {
            vox.value = 0;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                float lerpVal = voxelGrid[x, y].value > .1f ? voxelGrid[x, y].value : 0;

                Gizmos.color = Color.Lerp(Color.white, Color.red, lerpVal);
                Gizmos.DrawCube(voxelGrid[x, y].position, Vector3.one * voxelSize);

                Gizmos.DrawWireSphere(mousePos, voxelSize * 2);
            }
        }
    }
}


