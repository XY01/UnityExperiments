using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using XY01.TechArt.EditorUtils;

public class SDFBakerCompute : MonoBehaviour
{
    public ComputeShader sdfShader;
    public MeshRenderer[] targetMeshes;
    public RenderTexture outputTexture;

    public int resolution = 512;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer triangleBuffer;

    [Range(0,1)]
    public float boundsExpand = .1f;

    public bool adjustBoundToMeshes = false;
    public Bounds sdfBounds;

    public Material mat;
    
    [ContextMenu("Bake")]
    void Bake()
    {
        if (targetMeshes.Length == 0)
        {
            Debug.Log("No meshes assigned to SDF baker");
            return;
        }

        // Init render texture
        outputTexture = new RenderTexture(resolution,resolution,0)  // Init render texture, 0)
        {
            format = RenderTextureFormat.ARGB32,
            volumeDepth = resolution,
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            wrapMode = TextureWrapMode.Clamp,
            autoGenerateMips = false,
            useMipMap = false,
            name = "SDF Baked"
        };
        outputTexture.Create();
        mat.SetTexture("_SDF", outputTexture);
        
        // Set shader properties
        sdfShader.SetTexture(0, "Result", outputTexture);
        sdfShader.SetInt("width", outputTexture.width);
        sdfShader.SetInt("height", outputTexture.height);
        sdfShader.SetInt("depth", outputTexture.volumeDepth);

        // Set the target mesh bounds
        if(adjustBoundToMeshes)
            sdfBounds = targetMeshes[0].bounds;
        
        List<Vector3> vertPosList = new List<Vector3>();
        List<int> trianglesList = new List<int>();
        int triOffset = 0;
        for (int i = 0; i < targetMeshes.Length; i++)
        {
            if(adjustBoundToMeshes)
                sdfBounds.Encapsulate(targetMeshes[i].bounds);
            
            Mesh mesh = targetMeshes[i].GetComponent<MeshFilter>().mesh;
            
            Vector3[] vertices = mesh.vertices;
            for (int j = 0; j < vertices.Length; j++)
                vertPosList.Add(targetMeshes[i].transform.TransformPoint(vertices[j]));

            int[] triangles = mesh.triangles;
            for (int j = 0; j < triangles.Length; j++)
                trianglesList.Add(triangles[j] + triOffset);

            // Offset vert index by mesh vertex count so next mesh indecies line up with the 
            // vertex buffer
            triOffset += vertices.Length;
        }
        
        if(adjustBoundToMeshes)
            sdfBounds.Expand(boundsExpand);
        
        sdfShader.SetVector("boundsMin", sdfBounds.min);
        sdfShader.SetVector("boundsMax", sdfBounds.max);
        
        // Create compute buffers
        vertexBuffer = new ComputeBuffer(vertPosList.Count, 12); // 12 because sizeof(float3) = 3 * sizeof(float) = 3 * 4 = 12
        triangleBuffer = new ComputeBuffer(trianglesList.Count, 4); // sizeof(int) = 4

        print($"vert count {vertPosList.Count}  tri count {trianglesList.Count}");
        
        // Send data to GPU
        vertexBuffer.SetData(vertPosList);
        triangleBuffer.SetData(trianglesList);

        int sdfKernel = sdfShader.FindKernel("SDF");
        // Set buffers in the compute shader
        sdfShader.SetBuffer(sdfKernel, "vertexBuffer", vertexBuffer);
        sdfShader.SetBuffer(sdfKernel, "triangleBuffer", triangleBuffer);


        // Execute the compute shader
        sdfShader.Dispatch(sdfKernel, outputTexture.width / 8, outputTexture.height / 8, outputTexture.height / 8);

        SaveRenderTextures.Save3D(outputTexture, "/_Project/SDF Baker/Baked/SDF Test Bake",
            RenderTextureFormat.RFloat,
            TextureFormat.RFloat);
        
        // // Get the texture data back to the CPU
        // Texture2D tex = new Texture2D(outputTexture.width, outputTexture.height, TextureFormat.RGB24, false);
        // RenderTexture.active = outputTexture;
        // tex.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);
        // tex.Apply();
        //
        // // Reset active render texture
        // RenderTexture.active = null;
        //
        // // Convert the texture to byte array and process as you wish
        // byte[] bytes = tex.EncodeToPNG();
        //
        // // Dispose when finished to avoid memory leaks
        // Destroy(tex);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(sdfBounds.center, sdfBounds.size);
    }

    private void OnDestroy()
    {
        vertexBuffer.Dispose();
        triangleBuffer.Dispose();
    }
}

/*
 *
 *
float denisty = 0;
float transmission = 0;
float lightAccumulation = 0;
float finalLight = 0;
float t = 0;

for (int i = 0; i < numSteps; i++)
{
    float3 p = rayOrigin + rayDirection * t;
    float4 posMip = float4(p,0);
    float d = tex3Dlod(Volume, posMip).r;
    if(d < 0.01) 
    {
        return float4(p , 0); // we hit something, return its position as a color.
    }
    t += d; // scaling down gives better results
    
    if(t >= 1.0) 
    {
        break; // we didn't hit anything
    }
}

 return float4(0,0,0,0);
 */
