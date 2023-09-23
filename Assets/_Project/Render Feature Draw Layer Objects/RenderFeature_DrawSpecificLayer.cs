
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;
using static UnityEngine.XR.XRDisplaySubsystem;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

/// <summary>
/// Render a set layers depth and col to a scaled texture (i.e. 1/4 screen size) then compisits back over the top
/// </summary>
public class RenderFeature_DrawSpecificLayer : ScriptableRendererFeature
{
    //This pass is responsible for copying color to a specified destination
    class Custom_CopyFramePass : ScriptableRenderPass
    {
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }

        public void Setup(RTHandle source, RTHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Custom Copy Frame Pass");
            Blit(cmd, source, destination);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    class Custom_RenderLayerToTexturePass : ScriptableRenderPass
    {
        private RTHandle destColRTH;
        private RTHandle destDepthRTH;

        // Cam drawing settings inputs
        FilteringSettings filteringSettings;
        RenderStateBlock renderStateBlock;
        readonly List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();

        public Custom_RenderLayerToTexturePass(LayerMask layerMask)
        {
            // Setup drawing settings inputs
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
            shaderTagIds.Add(new ShaderTagId("LightweightForward"));
            renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        public void Setup(RTHandle destCol, RTHandle dstDepth)
        {
            destColRTH = destCol;
            destDepthRTH = dstDepth;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, SortingCriteria.CommonOpaque);

            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
                return;
                       
            CommandBuffer cmd = CommandBufferPool.Get("Render layer to texture pass");

            // Set the render target then execute the command buffer, otherwise it will still draw to the main cam col target
            CoreUtils.SetRenderTarget(cmd, destColRTH, destDepthRTH, ClearFlag.All);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }


    // Take in a scaled col and depth texture, set a blending material with those textures then blit to the main cam target
    class Custom_BlendScaledLayerOvertop : ScriptableRenderPass
    {
        private RTHandle scaledColRTH;
        private RTHandle scaledDepthRTH;

        Material blendMat;

        public void Setup(RTHandle destColRTH, RTHandle destDepthRTH, Material blendMat)
        {
            this.scaledColRTH = destColRTH;
            this.scaledDepthRTH = destDepthRTH;
            this.blendMat = blendMat;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
                return;

            blendMat.SetTexture("_ScaledCol", scaledColRTH);
            blendMat.SetTexture("_ScaledDepth", scaledDepthRTH);
            blendMat.SetTexture("_MainTex", renderingData.cameraData.renderer.cameraColorTargetHandle);
            blendMat.SetTexture("_CamFullSizeDepth", renderingData.cameraData.renderer.cameraDepthTargetHandle);

            CommandBuffer cmd = CommandBufferPool.Get("Blend Scaled Layer Overtop");
            //CoreUtils.SetRenderTarget(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle, ClearFlag.All);
            //context.ExecuteCommandBuffer(cmd);
           // cmd.Clear();

            //Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraColorTargetHandle, blendMat);
            Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraColorTargetHandle, blendMat, 0);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }



    // Render passes
    //private Custom_CopyFramePass copyDepthPass;
    Custom_RenderLayerToTexturePass renderLayerToTexturePass;
    Custom_BlendScaledLayerOvertop renderScaledLayerOvertopPass;


    // Scaled RT handles
    private RTHandle scaledDepthRTH;
    private RTHandle scaledColRTH;


    public int resolutionDivisor = 1;
    public LayerMask _layerMask;
    public RenderPassEvent renderPassEvent;

    public Material blendMat;

    // Debug mats to display output textures
    public Material outputMat_CopiedDepth;
    public Material outputMat_CopiedCol;

    /// <inheritdoc/>
    public override void Create()
    {
        //copyDepthPass = new Custom_CopyFramePass();
        //copyDepthPass.renderPassEvent = renderPassEvent;
        renderLayerToTexturePass = new Custom_RenderLayerToTexturePass(_layerMask);
        renderScaledLayerOvertopPass = new Custom_BlendScaledLayerOvertop();
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        //-- Scaled col RT
        var descriptorCol = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, RenderTextureFormat.ARGB32);
        RenderingUtils.ReAllocateIfNeeded(ref scaledColRTH, Vector2.one / resolutionDivisor, descriptorCol, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_scaledCol");
        outputMat_CopiedCol.SetTexture("_OutputTex", scaledColRTH);

        //-- Scaled depth RT
        var descriptorDepth = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, RenderTextureFormat.Depth, 16);
        RenderingUtils.ReAllocateIfNeeded(ref scaledDepthRTH, Vector2.one / resolutionDivisor, descriptorDepth, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_scaledDepth");
        outputMat_CopiedDepth.SetTexture("_OutputTex", scaledDepthRTH);


        //copyDepthPass.ConfigureClear(ClearFlag.None, Color.red);
        //copyDepthPass.Setup(renderer.cameraDepthTargetHandle, scaledDepthRTH);

        //renderLayerToTexturePass.ConfigureClear(ClearFlag.None, Color.red);

        renderLayerToTexturePass.Setup(scaledColRTH, scaledDepthRTH);
        renderScaledLayerOvertopPass.Setup(scaledColRTH, scaledDepthRTH, blendMat);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //renderer.EnqueuePass(copyDepthPass);
        //renderer.EnqueuePass(customRenderObjectsOnLayerPass);
        renderer.EnqueuePass(renderLayerToTexturePass);
        renderer.EnqueuePass(renderScaledLayerOvertopPass);
    }

    

    protected override void Dispose(bool disposing)
    {
        scaledColRTH?.Release();
        scaledDepthRTH?.Release();
    }
}


