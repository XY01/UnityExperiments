using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RayMarchRenderFeature : ScriptableRendererFeature
{
    public ComputeShader computeShader;
    private RayMarchRenderPass renderPass;
    
    private RTHandle depthRTH;
    private RTHandle colRTH;

    public override void Create()
    {
        renderPass = new RayMarchRenderPass(computeShader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderPass.SetCameraTargets(renderer.cameraColorTarget, renderer.cameraDepthTarget);
        renderer.EnqueuePass(renderPass);
    }
}

