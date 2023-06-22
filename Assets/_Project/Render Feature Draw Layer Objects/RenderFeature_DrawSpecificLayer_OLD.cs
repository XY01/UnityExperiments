using AmplifyShaderEditor;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

///// <summary>
///// Render a set layers depth and col to a scaled texture (i.e. 1/4 screen size) then compisits back over the top
///// </summary>
//public class RenderFeature_DrawSpecificLayer_OLD : ScriptableRendererFeature
//{
//    //This pass is responsible for copying color to a specified destination
//    class Custom_CopyFramePass : ScriptableRenderPass
//    {
//        private RTHandle source { get; set; }
//        private RTHandle destination { get; set; }

//        public void Setup(RTHandle source, RTHandle destination)
//        {
//            this.source = source;
//            this.destination = destination;
//        }

//        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//        {
//            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
//                return;

//            CommandBuffer cmd = CommandBufferPool.Get("Custom Copy Frame Pass");
//            Blit(cmd, source, destination);
//            context.ExecuteCommandBuffer(cmd);
//            CommandBufferPool.Release(cmd);
//        }
//    }





//    class Custom_RenderLayerToTexturePass : ScriptableRenderPass
//    {
//        private RTHandle destColRTH;
//        private RTHandle destDepthRTH;

//        // Cam drawing settings inputs
//        FilteringSettings filteringSettings;
//        RenderStateBlock renderStateBlock;
//        readonly List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();

//        public Custom_RenderLayerToTexturePass(LayerMask layerMask)
//        {
//            // Setup drawing settings inputs
//            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
//            shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
//            shaderTagIds.Add(new ShaderTagId("UniversalForward"));
//            shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
//            shaderTagIds.Add(new ShaderTagId("LightweightForward"));
//            renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
//        }

//        public void Setup(RTHandle destCol, RTHandle dstDepth)
//        {
//            destColRTH = destCol;
//            destDepthRTH = dstDepth;
//        }

//        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//        {
//            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, SortingCriteria.CommonOpaque);

//            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
//                return;
                       
//            CommandBuffer cmd = CommandBufferPool.Get("Render layer to texture pass");

//            // Set the render target then execute the command buffer, otherwise it will still draw to the main cam col target
//            CoreUtils.SetRenderTarget(cmd, destColRTH, destDepthRTH, ClearFlag.All);
//            context.ExecuteCommandBuffer(cmd);
//            cmd.Clear();

//            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
//            context.ExecuteCommandBuffer(cmd);
//            CommandBufferPool.Release(cmd);
//        }
//    }





//    class Custom_RenderObjectsOnLayerPass : ScriptableRenderPass
//    {
//        const string profilerTag = "Render Objects On Layer Pass";

//        PassSettings passSettings;

//        // Mat that we are going to blit the current camera texture to
//        private Material mat;
//        // Temp RT texture to blit too
//        private RTHandle tempRTHandle;

//        readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>();
//        FilteringSettings _filteringSettings;
//        RenderStateBlock _renderStateBlock;

//        RTHandle scaledCamDepthRTHandle;

//        // Constructor
//        public Custom_RenderObjectsOnLayerPass(PassSettings settings)
//        {
//            this.passSettings = settings;
//            renderPassEvent = passSettings.renderPassEvent;

//            // Now that this is verified within the Renderer Feature, it's already "trusted" here
//            mat = passSettings.combineMat;

//            _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings._layerMask);
//            // TODO Not sure what these are used for
//            _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
//            _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
//            _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
//            _shaderTagIds.Add(new ShaderTagId("LightweightForward"));

//            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

//            // _renderStateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
//        }

     
//        public void Setup(RTHandle source)
//        {
//            scaledCamDepthRTHandle = source;
//        }

//        // This method is called before executing the render pass.
//        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
//        // When empty this render pass will render to the active camera render target.
//        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
//        // The render pipeline will ensure target setup and clearing happens in a performant manner.
//        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
//        {
//            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
//            descriptor.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles
//            descriptor.colorFormat = RenderTextureFormat.ARGB32;
//            RenderingUtils.ReAllocateIfNeeded(ref tempRTHandle, Vector2.one / passSettings.ResolutionDivisor, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempRTex");
//        }


       

//        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//        {
//            if (mat == null)
//                return;            

//            SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
//            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIds, ref renderingData, sortingCriteria);

//            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
//            // Currently there's an issue which results in mismatched markers.
//            CommandBuffer cmd = CommandBufferPool.Get("Custom Render Objects On Layer Pass");
//            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
//            {
//                // Set the render target to the scaled down colour and scaled down copied depth
//                CoreUtils.SetRenderTarget(cmd, tempRTHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle, ClearFlag.Depth); // OG working
//                //CoreUtils.SetRenderTarget(cmd, tempRTHandle, copiedCamDepthHandle, ClearFlag.All); // OG working  
//                context.ExecuteCommandBuffer(cmd);
//                cmd.Clear();

//                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings, ref _renderStateBlock);
//                mat.SetTexture("_BaseCameraCol", tempRTHandle);
//                CoreUtils.SetRenderTarget(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle);
//                //cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
//                cmd.Blit(tempRTHandle, renderingData.cameraData.renderer.cameraColorTargetHandle, mat);                
//            }           

//            // Execute the command buffer and release it
//            context.ExecuteCommandBuffer(cmd);
//            cmd.Clear();
//            CommandBufferPool.Release(cmd);
//        }

//        // Cleanup any allocated resources that were created during the execution of this render pass.
//        public override void OnCameraCleanup(CommandBuffer cmd)
//        {
//            tempRTHandle.Release();
//            tempRTHandle = null;
//        }

//        public void Dispose()
//        {
//            // This seems vitally important, so why isn't it more prominently stated how it's intended to be used?
//            tempRTHandle?.Release();
//        }
//    }



//    [System.Serializable]
//    public class PassSettings
//    {
//        public Material combineMat;
//        public int ResolutionDivisor = 1;
//        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
//        public LayerMask _layerMask;
//    }

//    private Custom_CopyFramePass copyDepthPass;
//    private RTHandle scaledDepthRTH;
//    private RTHandle scaledColRTH;

//    public Material outputMat_CopiedDepth;
//    public Material outputMat_CopiedCol;


//    Custom_RenderObjectsOnLayerPass customRenderObjectsOnLayerPass;
//    public PassSettings passSettings = new PassSettings();

//    // This prevents attempted destruction of a manually-assigned material later
//    bool useDynamicTexture = false;

//    string _renderObjectId = "_RenderObjectID";


//    Custom_RenderLayerToTexturePass renderLayerToTexturePass;

//    /// <inheritdoc/>
//    public override void Create()
//    {
//        if (passSettings.combineMat == null)
//        {
//            passSettings.combineMat = CoreUtils.CreateEngineMaterial("RenderFeature/Combine");
//            useDynamicTexture = true;
//        }

//        this.customRenderObjectsOnLayerPass = new Custom_RenderObjectsOnLayerPass(passSettings);

//        copyDepthPass = new Custom_CopyFramePass();
//        copyDepthPass.renderPassEvent = passSettings.renderPassEvent;

//        renderLayerToTexturePass = new Custom_RenderLayerToTexturePass(passSettings._layerMask);
//    }

//    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
//    {
//        // Create scaled col and depth RT Handles
//        var descriptorCol = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, RenderTextureFormat.ARGB32);
//        RenderingUtils.ReAllocateIfNeeded(ref scaledColRTH, Vector2.one / passSettings.ResolutionDivisor, descriptorCol, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_scaledCol");
//        outputMat_CopiedCol.SetTexture("_OutputTex", scaledColRTH);

//        var descriptorDepth = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, RenderTextureFormat.Depth, 16);
//        RenderingUtils.ReAllocateIfNeeded(ref scaledDepthRTH, Vector2.one / passSettings.ResolutionDivisor, descriptorDepth, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_scaledDepth");
//        outputMat_CopiedDepth.SetTexture("_OutputTex", scaledDepthRTH);


//        //copyDepthPass.ConfigureClear(ClearFlag.None, Color.red);
//        //copyDepthPass.Setup(renderer.cameraDepthTargetHandle, scaledDepthRTH);

       
//        //customRenderObjectsOnLayerPass.Setup(scaledDepthRTH);

//        renderLayerToTexturePass.ConfigureClear(ClearFlag.None, Color.red);
//        renderLayerToTexturePass.Setup(scaledColRTH, scaledDepthRTH);
//    }

//    // Here you can inject one or multiple render passes in the renderer.
//    // This method is called when setting up the renderer once per-camera.
//    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//    {
//        //renderer.EnqueuePass(copyDepthPass);
//        //renderer.EnqueuePass(customRenderObjectsOnLayerPass);
//        renderer.EnqueuePass(renderLayerToTexturePass);
//    }

    

//    protected override void Dispose(bool disposing)
//    {
//        if(useDynamicTexture)
//        {
//            // Added this line to match convention for cleaning up materials
//            // ... But only for a dynamically-generated material
//            CoreUtils.Destroy(passSettings.combineMat);
//        }
//        customRenderObjectsOnLayerPass.Dispose();

//        scaledColRTH?.Release();
//        scaledDepthRTH?.Release();
//    }
//}

