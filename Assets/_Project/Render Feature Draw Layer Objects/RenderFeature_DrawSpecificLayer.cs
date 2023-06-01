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
public class RenderFeature_DrawSpecificLayer : ScriptableRendererFeature
{
    class RenderObjectsOnLayerPass : ScriptableRenderPass
    {
        const string profilerTag = "Render Objects On Layer Pass";

        PassSettings passSettings;

        // Mat that we are going to blit the current camera texture to
        private Material mat;
        // Temp RT texture to blit too
        private RTHandle tempRTHandle;
        private RTHandle tempDepthRTHandle;

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
            RenderingUtils.ReAllocateIfNeeded(ref tempRTHandle, Vector2.one / passSettings.ResolutionDivisor, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempRTex");

            //RenderTextureDescriptor descriptorDepth = renderingData.cameraData.cameraTargetDescriptor;
            //descriptorDepth.depthBufferBits = renderingData.cameraData.cameraTargetDescriptor.depthBufferBits;
            //RenderingUtils.ReAllocateIfNeeded(ref tempDepthRTHandle, Vector2.one / passSettings.ResolutionDivisor, descriptorDepth, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempDepthRTex");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (mat == null)
                return;            

            SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIds, ref renderingData, sortingCriteria);

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                //CoreUtils.SetRenderTarget(cmd, tempRTHandle, tempDepthRTHandle, ClearFlag.All);
                CoreUtils.SetRenderTarget(cmd, tempRTHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle, ClearFlag.All); // OG working
               
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings, ref _renderStateBlock);


                mat.SetTexture("_BaseCameraCol", renderingData.cameraData.renderer.cameraColorTargetHandle);
                CoreUtils.SetRenderTarget(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle);
                //cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                cmd.Blit(tempRTHandle, renderingData.cameraData.renderer.cameraColorTargetHandle, mat);                
            }           

            // Execute the command buffer and release it
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            tempRTHandle.Release();
            tempRTHandle = null;

            tempDepthRTHandle?.Release();
            tempDepthRTHandle = null;
        }

        public void Dispose()
        {
            // This seems vitally important, so why isn't it more prominently stated how it's intended to be used?
            tempRTHandle?.Release();
            tempDepthRTHandle?.Release();
        }
    }



    [System.Serializable]
    public class PassSettings
    {
        public Material combineMat;
        public int ResolutionDivisor = 1;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public LayerMask _layerMask;
    }

    RenderObjectsOnLayerPass renderPass;
    public PassSettings passSettings = new PassSettings();

    // This prevents attempted destruction of a manually-assigned material later
    bool useDynamicTexture = false;

    string _renderObjectId = "_RenderObjectID";

    /// <inheritdoc/>
    public override void Create()
    {
        if (passSettings.combineMat == null)
        {
            passSettings.combineMat = CoreUtils.CreateEngineMaterial("RenderFeature/Combine");
            useDynamicTexture = true;
        }

        this.renderPass = new RenderObjectsOnLayerPass(passSettings);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        if(useDynamicTexture)
        {
            // Added this line to match convention for cleaning up materials
            // ... But only for a dynamically-generated material
            CoreUtils.Destroy(passSettings.combineMat);
        }
        renderPass.Dispose();
    }
}


