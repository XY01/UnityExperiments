using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    public class CustomRenderPass : ScriptableRenderPass
    {
        private RenderTargetIdentifier mainRenderTarget;
        private RenderTargetHandle customRenderTarget;
        private Material combineMaterial;
        private string passTag;
        private LayerMask layerMask;

        public CustomRenderPass(Material combineMaterial, string passTag, LayerMask layerMask)
        {
            this.combineMaterial = combineMaterial;
            this.passTag = passTag;
            this.layerMask = layerMask;
            customRenderTarget.Init("_CustomRenderTarget");
        }

        public void SetRenderTarget(RenderTargetIdentifier mainRenderTarget)
        {
            this.mainRenderTarget = mainRenderTarget;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(customRenderTarget.id, cameraTextureDescriptor);
            ConfigureTarget(customRenderTarget.Identifier(), mainRenderTarget);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(passTag);
            using (new ProfilingScope(cmd, new ProfilingSampler(passTag)))
            {
                var drawingSettings = CreateDrawingSettings(ShaderTagId.none, ref renderingData, SortingCriteria.CommonOpaque);
                drawingSettings.SetShaderPassName(0, new ShaderTagId("UniversalForward"));
                drawingSettings.overrideMaterial = null;
                drawingSettings.overrideMaterialPassIndex = 0;
                drawingSettings.perObjectData = PerObjectData.None;
                var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                cmd.SetGlobalTexture("_CustomRenderTarget", customRenderTarget.Identifier());
                cmd.Blit(mainRenderTarget, mainRenderTarget, combineMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(customRenderTarget.id);
        }
    }



  
    [System.Serializable]
    public class Settings
    {
        public Material combineMaterial;
        public string passTag = "CustomRenderPass";
        public LayerMask layerMask = -1;
    }

    public Settings settings = new Settings();

    private CustomRenderPass customRenderPass;

    public override void Create()
    {
        customRenderPass = new CustomRenderPass(settings.combineMaterial, settings.passTag, settings.layerMask);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        customRenderPass.SetRenderTarget(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(customRenderPass);
    }
}



