using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Rendering/Pixel Render Pipeline")]
public class PixelRenderPipelineAsset : RenderPipelineAsset {

    [SerializeField] private bool srpBatching = true;
    [SerializeField] private bool dynamicBatching = true;
    [SerializeField] private bool instancing = true;
    [SerializeField] private ShadowSettings shadowSettings = default;
    
    private PixelRenderPipeline pixelRenderPipeline;
    private static readonly int CrossHatchTexture = Shader.PropertyToID("_CrossHatchTexture");

    protected override RenderPipeline CreatePipeline() {
        pixelRenderPipeline = new PixelRenderPipeline();
        GraphicsSettings.useScriptableRenderPipelineBatching = srpBatching;
        GraphicsSettings.lightsUseLinearIntensity = true;
        pixelRenderPipeline.DynamicBatching = dynamicBatching;
        pixelRenderPipeline.Instancing = instancing;
        pixelRenderPipeline.ShadowSettings = shadowSettings;
        
        return pixelRenderPipeline;
    }
}
