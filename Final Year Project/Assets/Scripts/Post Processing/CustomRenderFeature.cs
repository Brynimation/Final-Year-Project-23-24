using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material _material;
        int tintId = Shader.PropertyToID("_Temp");
        RenderTargetIdentifier src, tint;
        public CustomRenderPass() 
        {
            if (!_material) 
            {
                _material = CoreUtils.CreateEngineMaterial("PostProcessing/Test");
            }
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            src = renderingData.cameraData.renderer.cameraColorTarget;
            cmd.GetTemporaryRT(tintId, desc, FilterMode.Bilinear);
            tint = new RenderTargetIdentifier(tintId);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer buffer = CommandBufferPool.Get("CustomRenderFeature");
            VolumeStack volumes = VolumeManager.instance.stack;
            CustomPostProcessingEffect cppe = volumes.GetComponent<CustomPostProcessingEffect>();
            if (cppe.IsActive()) 
            {
                _material.SetColor("_OverlayColour", (Color) cppe.tintColour);
                _material.SetFloat("_Intensity", (float)cppe.tintIntensity);

                //blit from source to tint then tint back to source
                Blit(buffer, src, tint, _material, 0);
                Blit(buffer, tint, src);
            }
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tintId);
        }

    }

    private CustomRenderPass pass;
    public override void Create()
    {
        pass = new CustomRenderPass();
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }


}
