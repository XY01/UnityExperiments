using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CopyFramePass : ScriptableRenderPass
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
