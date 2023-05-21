using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaymarchedSmokeCompute : MonoBehaviour
{
    public ComputeShader shader;
    public Texture3D volumeTexture;    
    public Vector3 lightDirection = Vector3.down;
    public int resolution = 256;
    public RenderTexture resultTexture;

    int kernel;
    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        // Get the kernel index
        kernel = shader.FindKernel("SphereMarch");

        // Create the result texture
        resultTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
        resultTexture.enableRandomWrite = true;
        resultTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        resultTexture.volumeDepth = resolution;
        resultTexture.Create();
    }

    private void Update()
    {
        float fieldOfView = mainCam.fieldOfView * Mathf.Deg2Rad;

        // Set the shader parameters
        shader.SetTexture(kernel, "Result", resultTexture);
        shader.SetTexture(kernel, "VolumeTexture", volumeTexture);
        shader.SetVector("CameraPosition", mainCam.transform.position);
        shader.SetVector("CameraForward", mainCam.transform.forward);
        shader.SetVector("CameraUp", mainCam.transform.up);
        shader.SetVector("CameraRight", mainCam.transform.right);
        shader.SetFloat("FieldOfView", fieldOfView);
        shader.SetInts("ResultDimensions", new int[] { resolution, resolution, resolution });

        // Dispatch the shader
        shader.Dispatch(kernel, resolution / 8, resolution / 8, resolution / 8);
    }

    void OnDestroy()
    {
        // Clean up the result texture
        if (resultTexture != null)
        {
            resultTexture.Release();
            resultTexture = null;
        }
    }
}
