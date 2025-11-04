using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
// Following https://docs.unity3d.com/6000.1/Documentation/Manual/urp/renderer-features/create-custom-renderer-feature.html
public class BackgroundBlurRenderFeature : ScriptableRendererFeature {
    [SerializeField] private BlurSettings settings;
    [SerializeField] private Shader shader;
    private Material material;
    private BlurRenderPass blurRenderPass;
    // Like start but for shaders
    public override void Create() {
        if (shader == null) {
            return;
        }
        material = new Material(shader);
        blurRenderPass = new BlurRenderPass(material, settings);

        blurRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    
    // Unity calls this method every frame
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (blurRenderPass == null) {
            return;
        }
        if (renderingData.cameraData.cameraType == CameraType.Game) {
            Debug.Log("QUEIQUERHIUH");
            renderer.EnqueuePass(blurRenderPass);
        }
    }
    protected override void Dispose(bool disposing) {
        if (Application.isPlaying) {
            Destroy(material);
        } else {
            DestroyImmediate(material);
        }
    }


    public class BlurRenderPass : ScriptableRenderPass {
        private static readonly int horizontalBlurId = Shader.PropertyToID("_HorizontalBlur");
        private static readonly int verticalBlurId = Shader.PropertyToID("_VerticalBlur");
        private const string k_BlurTextureName = "_BlurTexture";
        private const string k_BackgroundTargetName = "_BackgroundTexture";
        private const string k_VerticalPassName = "BlurPassVertical";
        private const string k_HorizontalPassName = "BlurPassHorizontal";

        private BlurSettings defaultSettings;
        private Material material;

        private TextureDesc blurTextureDescriptor;

        // Constructor!
        public BlurRenderPass(Material material, BlurSettings defaultSettings) {
            this.material = material;
            this.defaultSettings = defaultSettings;
        }

        // Called every frame once for each camera, adds and configures render passes in the render graph. 
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            /*
              
            // Using a profiling scope ensures that our pass shows up in the Frame Debugger
            using var builder = renderGraph.AddRasterRenderPass<PassData>("Blur Background Pass", out var passData);
            // 1. GATHERING RESOURCES & DATA
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Get the camera's color target. This is what we will eventually draw TO.
            TextureHandle cameraColorTarget = resourceData.activeColorTexture;

            // 2. CREATING TEMPORARY TEXTURES FOR OUR EFFECT
            // We need one texture to draw the background objects into, and another for the blur ping-pong.
            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0; // We don't need depth for this.

            passData.backgroundTexture = builder.CreateTransientTexture(new TextureDesc(descriptor) { name = k_BackgroundTargetName });
            passData.tempBlurTexture = builder.CreateTransientTexture(new TextureDesc(descriptor) { name = k_BlurTextureName });

            // 3. DEFINING THE WORKFLOW

            // --- PASS 1: Render the specified layers into our temporary texture ---
            var blitParams = new RenderGraphUtils.BlitMaterialParameters(passData.backgroundTexture,)
            // Define the renderers we want to draw (based on layer mask).
            // This is the key step for isolating the objects!
            var rendererListDesc = new RendererListDesc(new ShaderTagId("UniversalForward"), cameraData.cullingResults, cameraData.camera) {
                layerMask = _settings.layersToBlur,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.CommonOpaque,
            };
            passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));
            builder.UseRendererList()
            // Set our background texture as the render target for this draw call.
            // We must clear it to transparent black so we don't get garbage from previous frames.
            builder.SetRenderAttachment(passData.backgroundTexture, 0, AccessFlags.Write, ClearFlag.Color, Color.clear);

            // Execute the draw call. This draws the sprites from the 'BlurrableBackground' layer.
            builder.DrawRendererList(passData.rendererList);


            // --- PASS 2 & 3: Perform the two-pass blur ---

            _settings.blurMaterial.SetFloat(BlurSizeID, _settings.blurStrength);

            // Vertical Blur: from our background texture -> to a temporary texture
            builder.BlitTexture(passData.backgroundTexture, passData.tempBlurTexture, _settings.blurMaterial, 0);

            // Horizontal Blur: from the temporary texture -> back to our background texture
            builder.BlitTexture(passData.tempBlurTexture, passData.backgroundTexture, _settings.blurMaterial, 1);


            // --- PASS 4: Composite the result back to the camera's main target ---

            // Now, blit our final blurred background texture to the camera's active color buffer.
            // Because our renderPassEvent is BeforeRenderingOpaques, this happens before
            // the rest of the scene is drawn.
            builder.SetRenderAttachment(cameraColorTarget, 0, AccessFlags.Write);
            builder.BlitTexture(passData.backgroundTexture, cameraColorTarget);

            // Set the pass's execution function
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));

            // The AddBlitPass method adds a vertical blur render graph pass that blits from the source texture (camera color in this case) to the destination texture using the first shader pass (the shader pass is defined in the last parameter).
            //RenderGraphUtils.BlitMaterialParameters paraVertical = new(srcCamColor, dst, material, 0);
            //renderGraph.AddBlitPass(paraVertical, k_VerticalPassName);
            //
            //// The AddBlitPass method adds a horizontal blur render graph pass that blits from the texture written by the vertical blur pass to the camera color texture. The method uses the second shader pass.
            //RenderGraphUtils.BlitMaterialParameters paraHorizontal = new(dst, srcCamColor, material, 1);
            //renderGraph.AddBlitPass(paraHorizontal, k_HorizontalPassName);
            */
        }
        private static void ExecutePass(PassData data, RasterGraphContext context) {
            
        }
        // The data that our pass needs to execute.
        private class PassData {
            public RendererListHandle rendererList;
            public TextureHandle backgroundTexture;
            public TextureHandle tempBlurTexture;
        }
        private void UpdateBlurSettings() {
            if (material == null) return;

            material.SetFloat(horizontalBlurId, defaultSettings.horizontalBlur);
            material.SetFloat(verticalBlurId, defaultSettings.verticalBlur);
        }
    }

    [Serializable]
    public class BlurSettings {
        [Range(0, 0.4f)] public float horizontalBlur;
        [Range(0, 0.4f)] public float verticalBlur;
    }
}