using UnityEngine;
using UnityEngine.Rendering;

public class Shadows {

    const string bufferName = "Shadows";

    CommandBuffer buffer = new CommandBuffer {
        name = bufferName
    };
    
    private static int _dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    
    private const int MaxShadowedDirectionalLightCount = 4;
    private static int _shadowedDirectionalLightCount;

    private ScriptableRenderContext context;

    private CullingResults cullingResults;

    private ShadowSettings settings;

    private struct ShadowedDirectionalLight {
        public int VisibleLightIndex;
    }

    private static ShadowedDirectionalLight[] _shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

    public void Setup (
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings settings
    ) {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        _shadowedDirectionalLightCount = 0;
    }
    
    public void Render () {
        if (_shadowedDirectionalLightCount > 0) {
            RenderDirectionalShadows();
        } else {
            buffer.GetTemporaryRT(
                _dirShadowAtlasId, 
                1, 
                1,
                32, 
                FilterMode.Bilinear, 
                RenderTextureFormat.Shadowmap
            );
        }
    }

    void RenderDirectionalShadows() {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(
            _dirShadowAtlasId, 
            atlasSize, 
            atlasSize, 
            32, 
            FilterMode.Bilinear, 
            RenderTextureFormat.Shadowmap);
        
        buffer.SetRenderTarget(_dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int split = _shadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;
        
        for (int i = 0; i < _shadowedDirectionalLightCount; i++) {
            RenderDirectionalShadows(i, split, tileSize);
        }
		
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows (int index, int split, int tileSize) {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(
            cullingResults, 
            light.VisibleLightIndex,
            BatchCullingProjectionType.Orthographic);
        
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.VisibleLightIndex, 
            0, 
            1, 
            Vector3.zero, 
            tileSize, 
            0f,
            out Matrix4x4 viewMatrix, 
            out Matrix4x4 projectionMatrix,
            out ShadowSplitData splitData
        );
        
        shadowSettings.splitData = splitData;
        SetTileViewport(index, split, tileSize);
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }
    
    public void ReserveDirectionalShadows (Light light, int visibleLightIndex) {
        if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && 
            light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
            _shadowedDirectionalLights[_shadowedDirectionalLightCount++] = new ShadowedDirectionalLight {
                VisibleLightIndex = visibleLightIndex
            };
        }
    }
    
    void SetTileViewport (int index, int split, float tileSize) {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
    }

    void ExecuteBuffer () {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    public void Cleanup () {
        buffer.ReleaseTemporaryRT(_dirShadowAtlasId);
        ExecuteBuffer();
    }
}