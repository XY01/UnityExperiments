using AmplifyShaderEditor;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

/// <summary>
/// A simple render feature that blits the current camera col buffer to a material then back the the camera col buffer.
/// </summary>
public class RenderFeature_DrawSpecificLayerWBlur : ScriptableRendererFeature
{
    class RenderObjectsOnLayerPass : ScriptableRenderPass
    {
        const string profilerTag = "Render Objects On Layer Pass";

        PassSettings passSettings;

        // Mat that we are going to blit the current camera texture to
        private Material mat;
        // Temp RT texture to blit too
        private RTHandle objectsOnLayerRT;

        readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>();
        FilteringSettings _filteringSettings;
        RenderStateBlock _renderStateBlock;

        // Constructor
        public RenderObjectsOnLayerPass(PassSettings settings)
        {
            this.passSettings = settings;
            renderPassEvent = passSettings.renderPassEvent;

            // Now that this is verified within the Renderer Feature, it's already "trusted" here
            mat = passSettings.combineMat;

            _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings._layerMask);
            // TODO Not sure what these are used for
            _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
            _shaderTagIds.Add(new ShaderTagId("LightweightForward"));

            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
           // _renderStateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles
            descriptor.colorFormat = RenderTextureFormat.ARGB32;

            RenderingUtils.ReAllocateIfNeeded(ref objectsOnLayerRT, Vector2.one, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ObjectsOnLayerRT");   
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // A reasonably common and simple safety net
            if (mat == null)
                return;
            

            SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIds, ref renderingData, sortingCriteria);

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                cmd.SetRenderTarget(objectsOnLayerRT.nameID, renderingData.cameraData.renderer.cameraDepthTargetHandle.nameID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings, ref _renderStateBlock);


                //mat.SetTexture("_RTex", renderingData.cameraData.renderer.cameraColorTargetHandle);
                //cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                //cmd.Blit(tempRTHandle, renderingData.cameraData.renderer.cameraColorTargetHandle, mat);
                
            }           

            // Execute the command buffer and release it
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //objectsOnLayerRT.Release();
            //objectsOnLayerRT = null;
        }

        public void Dispose()
        {
            // This seems vitally important, so why isn't it more prominently stated how it's intended to be used?
            //objectsOnLayerRT?.Release();
        }
    }


    class RenderPreviousRTHandleToScreen : ScriptableRenderPass
    {
        const string profilerTag = "Render Objects On Layer To Screen";

        PassSettings passSettings;

        // Mat that we are going to blit the current camera texture to
        private Material mat;
        // Temp RT texture to blit too
        private RTHandle objectsOnLayerRT;

        readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>();
        FilteringSettings _filteringSettings;
        RenderStateBlock _renderStateBlock;
        // Constructor
        public RenderPreviousRTHandleToScreen(PassSettings settings)
        {
            this.passSettings = settings;
            renderPassEvent = passSettings.renderPassEvent;

            // Now that this is verified within the Renderer Feature, it's already "trusted" here
            mat = passSettings.combineMat;

            _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings._layerMask);
            // TODO Not sure what these are used for
            _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
            _shaderTagIds.Add(new ShaderTagId("LightweightForward"));

            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            // _renderStateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles
            descriptor.colorFormat = RenderTextureFormat.ARGB32;

            //objectsOnLayerRT.I
            RenderingUtils.ReAllocateIfNeeded(ref objectsOnLayerRT, Vector2.one, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ObjectsOnLayerRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // A reasonably common and simple safety net
            if (mat == null)
                return;


            SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIds, ref renderingData, sortingCriteria);

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                //cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                //context.ExecuteCommandBuffer(cmd);
                //cmd.Clear();
                
                mat.SetTexture("_RTex", renderingData.cameraData.renderer.cameraColorTargetHandle);
                cmd.Blit(objectsOnLayerRT, renderingData.cameraData.renderer.cameraColorTargetHandle, mat);
            }

            // Execute the command buffer and release it
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //objectsOnLayerRT.Release();
          //  objectsOnLayerRT = null;

        }

        public void Dispose()
        {
            // This seems vitally important, so why isn't it more prominently stated how it's intended to be used?
          //  objectsOnLayerRT?.Release();
        }
    }

    //#region Kawase Blur

    //class KawaseBlurRenderPass : ScriptableRenderPass
    //{
    //    public Material BlurMaterial;
    //    public Material BlitMaterial;
    //    public int Passes;
    //    public int Downsample;

    //    int _tmpId1;
    //    int _tmpId2;

    //    int _tmpOGColId;

    //    RenderTargetIdentifier _tmpRT1;
    //    RenderTargetIdentifier _tmpRT2;
    //    RenderTargetIdentifier _tmpOGColRT;

    //    readonly int _blurSourceId;
    //    RenderTargetIdentifier _blurSourceIdentifier;

    //    readonly ProfilingSampler _profilingSampler;

    //    public KawaseBlurRenderPass(string profilerTag, int blurSourceId)
    //    {
    //        _profilingSampler = new ProfilingSampler(profilerTag);
    //        _blurSourceId = blurSourceId;
    //    }

    //    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    //    {
    //        _blurSourceIdentifier = new RenderTargetIdentifier(_blurSourceId);
    //    }

    //    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    //    {
    //        var width = cameraTextureDescriptor.width / Downsample;
    //        var height = cameraTextureDescriptor.height / Downsample;

    //        _tmpId1 = Shader.PropertyToID("tmpBlurRT1");
    //        _tmpId2 = Shader.PropertyToID("tmpBlurRT2");
    //        cmd.GetTemporaryRT(_tmpId1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
    //        cmd.GetTemporaryRT(_tmpId2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

    //        _tmpRT1 = new RenderTargetIdentifier(_tmpId1);
    //        _tmpRT2 = new RenderTargetIdentifier(_tmpId2);

    //        ConfigureTarget(_tmpRT1);
    //        ConfigureTarget(_tmpRT2);


    //        _tmpOGColId = Shader.PropertyToID("tmpOGColId");
    //        cmd.GetTemporaryRT(_tmpOGColId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
    //        _tmpOGColRT = new RenderTargetIdentifier(_tmpOGColId);
    //        ConfigureTarget(_tmpOGColRT);
    //    }

    //    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    //    {
    //        // Create an RTHandle with the same descriptor as the camera target
    //        RTHandle rtHandle = RTHandles.Alloc(renderingData.cameraData.cameraTargetDescriptor);

    //        // Get the RenderTexture from the RTHandle
    //        RenderTexture renderTexture = rtHandle.rt;

    //        // Set the RenderTexture to the material texture slot
    //        BlitMaterial.SetTexture("_RTex", renderTexture);


    //        RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
    //        opaqueDesc.depthBufferBits = 0;

    //        CommandBuffer cmd = CommandBufferPool.Get();
    //        using (new ProfilingScope(cmd, _profilingSampler))
    //        {
    //            // first pass
    //            cmd.SetGlobalFloat("_offset", 1.5f);


    //            cmd.Blit(_blurSourceIdentifier, _tmpRT1, BlurMaterial);

    //            for (var i = 1; i < Passes - 1; i++)
    //            {
    //                cmd.SetGlobalFloat("_offset", 0.5f + i);
    //                cmd.Blit(_tmpRT1, _tmpRT2, BlurMaterial);

    //                // pingpong
    //                var rttmp = _tmpRT1;
    //                _tmpRT1 = _tmpRT2;
    //                _tmpRT2 = rttmp;
    //            }

    //            // final pass
    //            cmd.SetGlobalFloat("_offset", 0.5f + Passes - 1f);
    //            cmd.Blit(_tmpRT1, renderingData.cameraData.renderer.cameraColorTarget, BlitMaterial);
    //            // cmd.Blit(renderingData.cameraData.renderer.cameraColorTarget, renderingData.cameraData.renderer.cameraColorTarget);
    //        }

    //        context.ExecuteCommandBuffer(cmd);
    //        cmd.Clear();

    //        CommandBufferPool.Release(cmd);
    //        rtHandle.Release();
    //    }

    //    public override void OnCameraCleanup(CommandBuffer cmd)
    //    {
    //        cmd.ReleaseTemporaryRT(_tmpId1);
    //        cmd.ReleaseTemporaryRT(_tmpId2);
    //    }

    //    public void Dispose()
    //    {
    //        // This seems vitally important, so why isn't it more prominently stated how it's intended to be used?
    //        //tempRTHandle?.Release();
    //    }
    //}

    //#endregion

    [System.Serializable]
    public class PassSettings
    {
        public Material combineMat;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public LayerMask _layerMask;
        public Material blurMaterial;
        public int blurPasses = 1;
    }

    public PassSettings passSettings = new PassSettings();


    RenderObjectsOnLayerPass renderObjectsPass;

    RenderPreviousRTHandleToScreen renderObjsToScreen;
    // KawaseBlurRenderPass blurPass;

    RenderTexture rt;

    // This prevents attempted destruction of a manually-assigned material later
    bool useDynamicTexture = false;

    string _renderObjectId = "_RenderObjectID";

    /// <inheritdoc/>
    public override void Create()
    {
        if (passSettings.combineMat == null)
        {
           
            useDynamicTexture = true;
        }

        passSettings.combineMat = CoreUtils.CreateEngineMaterial("RenderFeature/Combine");
        passSettings.blurMaterial = CoreUtils.CreateEngineMaterial("Custom/RenderFeature/KawaseBlur");

        this.renderObjectsPass = new RenderObjectsOnLayerPass(passSettings);

        this.renderObjsToScreen = new RenderPreviousRTHandleToScreen(passSettings);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(renderObjectsPass);
        renderer.EnqueuePass(renderObjsToScreen);
        // renderer.EnqueuePass(blurPass);
        //blurPass = new KawaseBlurRenderPass("KawaseBlur", renderTargetId)
        //{
        //    Downsample = 1,
        //    Passes = passSettings.blurPasses,
        //    BlitMaterial = _blitMaterial,
        //    BlurMaterial = passSettings.blurMaterial,
        //};
    }

    protected override void Dispose(bool disposing)
    {
        if(useDynamicTexture)
        {
            // Added this line to match convention for cleaning up materials
            // ... But only for a dynamically-generated material
            CoreUtils.Destroy(passSettings.combineMat);
        }

        renderObjectsPass.Dispose();
        renderObjsToScreen.Dispose();
        // blurPass.Dispose();
    }
}


