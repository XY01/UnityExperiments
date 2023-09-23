
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A simple render feature that blits the current camera col buffer to a material then back the the camera col buffer.
/// </summary>
public class RenderFeature_MaterialBlit : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        const string profilerTag = "Material Blit Pass";

        PassSettings passSettings;

        // Mat that we are going to blit the current camera texture to
        private Material mat;
        // Ref to current camera texture. 
        private RTHandle colorBuffer;
        // Temp RT texture to blit too
        private RTHandle tempBuffer;

        // Constructor
        public CustomRenderPass(PassSettings settings)
        {
            this.passSettings = settings;
            renderPassEvent = passSettings.renderPassEvent;

            // Now that this is verified within the Renderer Feature, it's already "trusted" here
            mat = passSettings.material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles

            // Enable these if your pass requires access to the CameraDepthTexture or the CameraNormalsTexture
            // ConfigureInput(ScriptableRenderPassInput.Depth);
            // ConfigureInput(ScriptableRenderPassInput.Normal);

            colorBuffer = renderingData.cameraData.renderer.cameraColorTargetHandle;

            RenderingUtils.ReAllocateIfNeeded(ref tempBuffer, Vector2.one, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TemporaryBuffer");
        }

      
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // A reasonably common and simple safety net
            if (mat == null)
            {
                return;
            }

        
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                // Write to our temp buffer using our mat then write back to the camer col buffer
                Blit(cmd, colorBuffer, tempBuffer, mat, 0); // shader pass 0
                Blit(cmd, tempBuffer, colorBuffer); // shader pass 1
            }           

            // Execute the command buffer and release it
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            // Mentioned in the "Upgrade Guide" but pretty much only seen in "official" examples
            // in "DepthNormalOnlyPass"
            // https://github.com/Unity-Technologies/Graphics/blob/9ff23b60470c39020d8d474547bc0e01dde1d9e1/Packages/com.unity.render-pipelines.universal/Runtime/Passes/DepthNormalOnlyPass.cs
            colorBuffer = null;
        }

        public void Dispose()
        {
            // This seems vitally important, so why isn't it more prominently stated how it's intended to be used?
            tempBuffer?.Release();
        }
    }



    [System.Serializable]
    public class PassSettings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    CustomRenderPass renderPass;
    public PassSettings passSettings = new PassSettings();

    // This prevents attempted destruction of a manually-assigned material later
    bool useDynamicTexture = false;
   


    /// <inheritdoc/>
    public override void Create()
    {
        if (passSettings.material == null)
        {
            passSettings.material = CoreUtils.CreateEngineMaterial("RenderFeature/Desaturate");
            useDynamicTexture = true;
        }

        this.renderPass = new CustomRenderPass(passSettings);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //renderPass.SetSource(renderer);
        renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        if(useDynamicTexture)
        {
            // Added this line to match convention for cleaning up materials
            // ... But only for a dynamically-generated material
            CoreUtils.Destroy(passSettings.material);
        }
        renderPass.Dispose();
    }
}


