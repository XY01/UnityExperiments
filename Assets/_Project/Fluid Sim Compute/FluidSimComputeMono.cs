using UnityEditor.Rendering;
using UnityEngine;

public class FluidSimComputeMono : MonoBehaviour
{
    public ComputeShader FluidComputeShader;
    private RenderTexture velocityTexture;
    private RenderTexture tempVelocityTexture;
    private RenderTexture densityTexture;
    private RenderTexture tempDensityTexture;
    private RenderTexture pressureTexture;
    private RenderTexture divergenceTexture;

    private int kernelHandle;

    private const int RES = 512;

    public Material Material;

    void Start() 
    {
        // Initialize all our textures/buffers
        velocityTexture = CreateRenderTexture();
        tempVelocityTexture = CreateRenderTexture();
        densityTexture = CreateRenderTexture();
        tempDensityTexture = CreateRenderTexture();
        pressureTexture = CreateRenderTexture();
        divergenceTexture = CreateRenderTexture();
        
        // Set the sampler state for the compute shader
       

        kernelHandle = FluidComputeShader.FindKernel("FluidSim2D");
        
        Material.SetTexture("_Velocity", velocityTexture);
    }

    void Update() 
    {
        // Set buffers
        FluidComputeShader.SetTexture(kernelHandle, "Velocity", velocityTexture);
        FluidComputeShader.SetTexture(kernelHandle, "TempVelocity", tempVelocityTexture);
        
        FluidComputeShader.SetTexture(kernelHandle, "Density", densityTexture);
        FluidComputeShader.SetTexture(kernelHandle, "TempDensity", tempDensityTexture);
        
        FluidComputeShader.SetTexture(kernelHandle, "Pressure", pressureTexture);
        FluidComputeShader.SetTexture(kernelHandle, "Divergence", divergenceTexture);

        // Execute the compute shader
        FluidComputeShader.Dispatch(kernelHandle, RES / 8, RES / 8, 1);

        // Swap the textures for the next iteration
        Swap(ref velocityTexture, ref tempVelocityTexture);
        Swap(ref densityTexture, ref tempDensityTexture);
    }

    private RenderTexture CreateRenderTexture() {
        RenderTexture rt = new RenderTexture(RES, RES, 24);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    private void Swap(ref RenderTexture a, ref RenderTexture b) {
        RenderTexture temp = a;
        a = b;
        b = temp;
    }

    void OnDestroy() {
        // Clean up the textures once done
        velocityTexture.Release();
        tempVelocityTexture.Release();
        densityTexture.Release();
        tempDensityTexture.Release();
        pressureTexture.Release();
        divergenceTexture.Release();
    }
}