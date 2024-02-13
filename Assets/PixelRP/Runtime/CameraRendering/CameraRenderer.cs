using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public partial class CameraRenderer {
    private const string BufferName = "Render Camera";

    private CommandBuffer buffer = new() {
        name = BufferName
    };

    private static ShaderTagId _defaultShaderTagId = new("SRPDefaultUnlit");
    private static ShaderTagId _forwardShaderTagId = new("PixelForwardLit");
    private static ShaderTagId _geometryShaderTagId = new("Geometry");

    private static int _geometry = Shader.PropertyToID("_Geometry");
    private static int _fx = Shader.PropertyToID("_FX");
    private static int _albedo = Shader.PropertyToID("_Albedo");
    private static int _normal = Shader.PropertyToID("_Normal");
    private static int _position = Shader.PropertyToID("_Position");
    private static int _offset = Shader.PropertyToID("_Offset");
    private static int _edge = Shader.PropertyToID("_Edge");

    private static int _cameraForward = Shader.PropertyToID("_camera_forward");
    
    private Material deferredMaterial = new Material(Shader.Find("Pixel RP/DeferredLit"));

    private ScriptableRenderContext context;

    private Camera camera;

    private Lighting lighting = new();

    private SortingSettings sortingSettings;
    private DrawingSettings drawingSettings;

    private CullingResults cullingResults;
    
    private RenderTargetIdentifier[] geometryBuffer;

    private RenderTexture fxRenderTexture;

    public void Render(ScriptableRenderContext context, Camera camera, bool dynamicBatching, bool instancing, ShadowSettings shadowSettings) {
        
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance)) {
            return;
        }
        
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(SampleName);
        Setup(dynamicBatching, instancing);
        DrawDeferredGeometry();
        DrawForwardGeometry();
        DrawGizmos();

        buffer.Blit(_geometry, BuiltinRenderTextureType.CameraTarget);
        CleanUp();
        Submit();
    }

    private void Setup(bool dynamicBatching, bool instancing) {

        if (deferredMaterial == null) {
            deferredMaterial = new Material(Shader.Find("Pixel RP/DeferredLit"));
        }
        
        buffer.SetGlobalVector(_cameraForward, camera.transform.forward);
        context.SetupCameraProperties(camera);
        
        sortingSettings = new SortingSettings(camera);
        drawingSettings = new DrawingSettings {
            enableDynamicBatching = dynamicBatching,
            enableInstancing = instancing,
            sortingSettings = sortingSettings
        };

        CameraClearFlags flags = camera.clearFlags;
        bool clearDepth = flags <= CameraClearFlags.Depth;
        bool clearColor = flags <= CameraClearFlags.Color;
        Color backGroundColor = flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear;
        
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        buffer.ClearRenderTarget(clearDepth, clearColor, backGroundColor);

        SetupRenderTexture(ref _geometry, false, 24, RenderTextureFormat.ARGB32);
        buffer.ClearRenderTarget(clearDepth, clearColor, backGroundColor);
        
        SetupRenderTexture(ref _fx, false, 0, RenderTextureFormat.ARGBFloat);
        buffer.ClearRenderTarget(clearDepth, clearColor, Color.clear);
        
        SetupRenderTexture(ref _normal, false, 0, RenderTextureFormat.ARGBFloat);
        SetupRenderTexture(ref _position, false, 0, RenderTextureFormat.ARGBFloat);
        SetupRenderTexture(ref _offset, false, 0, RenderTextureFormat.ARGBFloat);
        SetupRenderTexture(ref _edge, false, 0, RenderTextureFormat.ARGBFloat);
        SetupRenderTexture(ref _albedo, false, 0, RenderTextureFormat.ARGB32);
        SetupGeometryBuffer();
        buffer.ClearRenderTarget(clearDepth, clearColor, backGroundColor);
        
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    private bool Cull(float maxShadowDistance) {
        if (!camera.TryGetCullingParameters(out ScriptableCullingParameters p)) return false;
        p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
        cullingResults = context.Cull(ref p);
        return true;
    }

    private void DrawFX() {

    }

    private void DrawDeferredGeometry() {
        buffer.SetRenderTarget(geometryBuffer, _geometry);
        ExecuteBuffer();
        buffer.BeginSample(SampleName);
        
        drawingSettings.SetShaderPassName(0, _geometryShaderTagId);
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        drawingSettings.SetShaderPassName(0, ShaderTagId.none);
        
        buffer.Blit(_albedo, _geometry, deferredMaterial, 1);
        buffer.EndSample(SampleName);
    }

    private void DrawForwardGeometry() {
        buffer.SetRenderTarget(_geometry);
        ExecuteBuffer();
        buffer.BeginSample(SampleName);
        drawingSettings.SetShaderPassName(0, _forwardShaderTagId);
        drawingSettings.SetShaderPassName(1, _defaultShaderTagId);
        
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        context.DrawSkybox(camera);
        
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        buffer.EndSample(SampleName);
        DrawUnsupportedShaders();
        
        drawingSettings.SetShaderPassName(0, ShaderTagId.none);
        drawingSettings.SetShaderPassName(1, ShaderTagId.none);
    }

    private void SetupGeometryBuffer() {
        geometryBuffer = new RenderTargetIdentifier[] { 
            _albedo, 
            _normal,
            _position,
            _offset,
            _edge
        };
        
        buffer.SetRenderTarget(geometryBuffer, _geometry);
    }

    private void SetupRenderTexture(
        ref int renderTextureId,
        bool antiAliasing, 
        int depth, 
        RenderTextureFormat format) {
        
        buffer.GetTemporaryRT(
            renderTextureId, 
            Screen.width, 
            Screen.height, 
            depth, 
            FilterMode.Bilinear, 
            format, 
            RenderTextureReadWrite.Default,
            antiAliasing ? QualitySettings.antiAliasing : 1);
        
        buffer.SetRenderTarget(renderTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
    }

    private void Submit () {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
            
        context.Submit();
    }

    private void ExecuteBuffer () {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void CleanUp() {
        buffer.ReleaseTemporaryRT(_geometry);
        buffer.ReleaseTemporaryRT(_fx);
        buffer.ReleaseTemporaryRT(_albedo);
        buffer.ReleaseTemporaryRT(_normal);
        buffer.ReleaseTemporaryRT(_position);
        buffer.ReleaseTemporaryRT(_offset);
        buffer.ReleaseTemporaryRT(_edge);
        lighting.Cleanup();
    }
}