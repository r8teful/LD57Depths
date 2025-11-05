using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
// Following https://docs.unity3d.com/6000.1/Documentation/Manual/urp/renderer-features/create-custom-renderer-feature.html
public class BackgroundBlurSimple : ScriptableRendererFeature {
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
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            TextureHandle srcCamColor = resourceData.activeColorTexture;
            blurTextureDescriptor = srcCamColor.GetDescriptor(renderGraph);
            blurTextureDescriptor.name = k_BlurTextureName;
            blurTextureDescriptor.depthBufferBits = 0;
            var dst = renderGraph.CreateTexture(blurTextureDescriptor);

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // The following line ensures that the render pass doesn't blit
            // from the back buffer.
            if (resourceData.isActiveTargetBackBuffer)
                return;
            // Update the blur settings in the material
            UpdateBlurSettings();

            // This check is to avoid an error from the material preview in the scene
            if (!srcCamColor.IsValid() || !dst.IsValid())
                return;

            // The AddBlitPass method adds a vertical blur render graph pass that blits from the source texture (camera color in this case) to the destination texture using the first shader pass (the shader pass is defined in the last parameter).
            RenderGraphUtils.BlitMaterialParameters paraVertical = new(srcCamColor, dst, material, 0);
            renderGraph.AddBlitPass(paraVertical, k_VerticalPassName);

            // The AddBlitPass method adds a horizontal blur render graph pass that blits from the texture written by the vertical blur pass to the camera color texture. The method uses the second shader pass.
            RenderGraphUtils.BlitMaterialParameters paraHorizontal = new(dst, srcCamColor, material, 1);
            renderGraph.AddBlitPass(paraHorizontal, k_HorizontalPassName);
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