using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    partial void DrawGizmosBeforeFX();

    partial void DrawGizmosAfterFX();

    partial void DrawGBufferChannels();
    partial void DrawUnsupportedShaders();
    
    partial void DrawGizmos ();

    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();
    
#if UNITY_EDITOR

    private static ShaderTagId[] _legacyShaderTagIds = {
        new("Always"),
        new("ForwardBase"),
        new("PrepassBase"),
        new("Vertex"),
        new("VertexLMRGBM"),
        new("VertexLM")
    };

    private static Material _errorMaterial;

    private string SampleName
    { get; set; }

    partial void DrawGBufferChannels() {
        buffer.Blit(_albedo, _geometry);
        buffer.Blit(_normal, _geometry);
        buffer.Blit(_position, _geometry);
        buffer.Blit(_offset, _geometry);
        buffer.Blit(_edge, _geometry);
        buffer.Blit(_highlights, _geometry);
    }
        
    partial void DrawUnsupportedShaders()
    {
        if (_errorMaterial == null) {
            _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        
        DrawingSettings drawingSettings = new DrawingSettings {
            overrideMaterial = _errorMaterial,
            sortingSettings = new SortingSettings(camera)
        };
        for (int i = 0; i < _legacyShaderTagIds.Length; i++) {
            drawingSettings.SetShaderPassName(i, _legacyShaderTagIds[i]);
        }
        
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    
    partial void DrawGizmos ()
    {
        if (!Handles.ShouldRenderGizmos()) return;
        
        context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
    }
    
    partial void PrepareForSceneWindow () {
        if (camera.cameraType == CameraType.SceneView) {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }
    
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
	const string SampleName = bufferName;
#endif
}