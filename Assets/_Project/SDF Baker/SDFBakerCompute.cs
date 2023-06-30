using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFBakerCompute : MonoBehaviour
{
    public ComputeShader sdfShader;
    public MeshRenderer targetMesh;
    public RenderTexture outputTexture;

    public int resolution = 512;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer triangleBuffer;
    
    [ContextMenu("Bake")]
    void Bake()
    {
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
        
        // Set shader properties
        sdfShader.SetTexture(0, "Result", outputTexture);
        sdfShader.SetInt("width", outputTexture.width);
        sdfShader.SetInt("height", outputTexture.height);
        sdfShader.SetInt("depth", outputTexture.volumeDepth);

        // Set the target mesh bounds
        Bounds sdfBounds = new Bounds(targetMesh.bounds.center, targetMesh.bounds.size);
        sdfBounds.Expand(.8f);
        sdfShader.SetVector("boundsMin", sdfBounds.min);
        sdfShader.SetVector("boundsMax", sdfBounds.max);
        
        Mesh mesh = targetMesh.GetComponent<MeshFilter>().mesh;

        // Convert mesh data to arrays
        // Convert verts to world space
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = targetMesh.transform.TransformPoint(vertices[i]);
        }
        
        int[] triangles = mesh.triangles;

        // Create compute buffers
        vertexBuffer = new ComputeBuffer(vertices.Length, 12); // 12 because sizeof(float3) = 3 * sizeof(float) = 3 * 4 = 12
        triangleBuffer = new ComputeBuffer(triangles.Length, 4); // sizeof(int) = 4

        // Send data to GPU
        vertexBuffer.SetData(vertices);
        triangleBuffer.SetData(triangles);

        int sdfKernel = sdfShader.FindKernel("SDF");
        // Set buffers in the compute shader
        sdfShader.SetBuffer(sdfKernel, "vertexBuffer", vertexBuffer);
        sdfShader.SetBuffer(sdfKernel, "triangleBuffer", triangleBuffer);


        // Execute the compute shader
        sdfShader.Dispatch(sdfKernel, outputTexture.width / 8, outputTexture.height / 8, outputTexture.height / 8);

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

    private void OnDestroy()
    {
        vertexBuffer.Dispose();
        triangleBuffer.Dispose();
    }
}
