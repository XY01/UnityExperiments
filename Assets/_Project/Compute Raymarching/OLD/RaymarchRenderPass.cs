using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RayMarchRenderPass : ScriptableRenderPass
{
    private ComputeShader computeShader;
    private int kernel;

    private RenderTargetIdentifier cameraColorTargetId;
    private RenderTargetIdentifier cameraDepthTargetId;
    private RenderTexture resultTexture;

    public RayMarchRenderPass(ComputeShader shader)
    {
        computeShader = shader;
        kernel = computeShader.FindKernel("CSMain");

        resultTexture = new RenderTexture(Screen.width, Screen.height, 24)
        {
            enableRandomWrite = true
        };
        resultTexture.Create();
    }

    public void SetCameraTargets(RenderTargetIdentifier colorTargetId, RenderTargetIdentifier depthTargetId)
    {
        cameraColorTargetId = colorTargetId;
        cameraDepthTargetId = depthTargetId;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // CommandBuffer cmd = CommandBufferPool.Get("RayMarchPass");
        //
        // computeShader.SetTexture(kernel, "_Result", resultTexture);
        // computeShader.SetTexture(kernel, "_DepthTexture", cameraDepthTargetId);
        // computeShader.SetFloat("_StepSize", 1.0f / 256);
        // computeShader.Dispatch(kernel, Screen.width / 8, Screen.height / 8, 1);
        //
        // cmd.Blit(resultTexture, cameraColorTargetId);
        //
        // context.ExecuteCommandBuffer(cmd);
        // cmd.Clear();
        // CommandBufferPool.Release(cmd);
    }
}