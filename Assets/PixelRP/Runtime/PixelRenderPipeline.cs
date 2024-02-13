using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PixelRenderPipeline : RenderPipeline
{
    private static CameraRenderer _cameraRenderer = new CameraRenderer();

    public bool DynamicBatching { get; set; } = true;
    public bool Instancing { get; set; } = true;
    public ShadowSettings ShadowSettings { get; set; }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras) { }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras) {
        foreach (Camera c in cameras) {
            _cameraRenderer.Render(context, c, DynamicBatching, Instancing, ShadowSettings);
        }
    }
}
