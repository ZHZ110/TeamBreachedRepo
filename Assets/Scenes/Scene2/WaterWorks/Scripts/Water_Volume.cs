using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class Water_Volume : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material _material;
        private string m_PassName = "Water Volume Pass";

        public CustomRenderPass(Material mat)
        {
            _material = mat;
        }

        // Modern Render Graph approach
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            if (cameraData.cameraType == CameraType.Reflection) return;

            // Get the camera color target
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle cameraColorTexture = resourceData.activeColorTexture;

            // Create temporary texture descriptor
            RenderTextureDescriptor tempDesc = cameraData.cameraTargetDescriptor;
            tempDesc.depthBufferBits = 0;

            // Create temporary texture
            TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, tempDesc, "_TemporaryWaterTexture", false);

            // Add render pass
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_PassName, out var passData))
            {
                passData.material = _material;
                passData.cameraColorTexture = cameraColorTexture;
                passData.tempTexture = tempTexture;

                builder.UseTexture(cameraColorTexture, AccessFlags.Read);
                builder.UseTexture(tempTexture, AccessFlags.Write);
                builder.SetRenderAttachment(tempTexture, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Blit from camera color to temp texture with material
                    Blitter.BlitTexture(context.cmd, data.cameraColorTexture, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Second pass: blit back to camera color
            using (var builder = renderGraph.AddRasterRenderPass<PassData2>(m_PassName + " Copy Back", out var passData2))
            {
                passData2.sourceTexture = tempTexture;
                passData2.targetTexture = cameraColorTexture;

                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.UseTexture(cameraColorTexture, AccessFlags.Write);
                builder.SetRenderAttachment(cameraColorTexture, 0);

                builder.SetRenderFunc((PassData2 data, RasterGraphContext context) =>
                {
                    // Blit from temp texture back to camera color
                    Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), null, 0);
                });
            }
        }

        // Fallback for compatibility mode (when Render Graph is disabled)
        [System.Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Left empty for compatibility
        }

        [System.Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Fallback implementation for compatibility mode
            if (renderingData.cameraData.cameraType != CameraType.Reflection && _material != null)
            {
                CommandBuffer cmd = CommandBufferPool.Get(m_PassName);

                // Simple fallback - just apply the material directly
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                var tempRT = RenderTexture.GetTemporary(renderingData.cameraData.cameraTargetDescriptor);

                cmd.Blit(source, tempRT, _material);
                cmd.Blit(tempRT, source);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                RenderTexture.ReleaseTemporary(tempRT);
            }
        }

        private class PassData
        {
            public Material material;
            public TextureHandle cameraColorTexture;
            public TextureHandle tempTexture;
        }

        private class PassData2
        {
            public TextureHandle sourceTexture;
            public TextureHandle targetTexture;
        }
    }

    [System.Serializable]
    public class Settings
    {
        public Material material = null;
        public RenderPassEvent renderPass = RenderPassEvent.AfterRenderingSkybox;
    }

    public Settings settings = new Settings();
    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        if (settings.material == null)
        {
            settings.material = (Material)Resources.Load("Water_Volume");
        }
        m_ScriptablePass = new CustomRenderPass(settings.material);
        m_ScriptablePass.renderPassEvent = settings.renderPass;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}