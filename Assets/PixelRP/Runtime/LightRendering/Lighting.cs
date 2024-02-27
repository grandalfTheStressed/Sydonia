using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting {
    
    private const int MaxDirLightCount = 4;
    private const int MaxPunctualLights = 512;

    private static int _dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int _dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int _dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static int _dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    
    private static int _punctualLightCountId = Shader.PropertyToID("_PunctualLightCount");
    private static int _punctualLightColorsId = Shader.PropertyToID("_PunctualLightColors");
    private static int _punctualLightPositionsId = Shader.PropertyToID("_PunctualLightPositions");
    private static int _punctualLightDirectionsId = Shader.PropertyToID("_PunctualLightDirections");
    private static int _punctualLightSpotAnglesId = Shader.PropertyToID("_PunctualLightSpotAngles");
    private static int _punctualLightShadowDataId = Shader.PropertyToID("_PunctualLightShadowData");
    
    private static Vector4[] _dirLightColors = new Vector4[MaxDirLightCount];
    private static Vector4[] _dirLightDirections = new Vector4[MaxDirLightCount];
    private static Vector4[] _dirLightShadowData = new Vector4[MaxDirLightCount];
    
    private static Vector4[] _punctualLightColors = new Vector4[MaxPunctualLights];
    private static Vector4[] _punctualLightPositions = new Vector4[MaxPunctualLights];
    private static Vector4[] _punctualLightDirections = new Vector4[MaxPunctualLights];
    private static Vector4[] _punctualLightSpotAngles = new Vector4[MaxPunctualLights];
    private static Vector4[] _punctualLightShadowData = new Vector4[MaxPunctualLights];

    private const string BufferName = "Lighting";

    private CommandBuffer buffer = new CommandBuffer {
        name = BufferName
    };

    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    
    private static Shadows _shadows = new();
	
    public void Setup (ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings) {
        this.context = context;
        this.cullingResults = cullingResults;
        buffer.BeginSample(BufferName);
        _shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        _shadows.Render();
        buffer.EndSample(BufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void SetupLights () {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        
        int dirLightCount = 0;
        int puncLightCount = 0;
        for (var i = 0; i < visibleLights.Length; i++) {
            var vl = visibleLights[i];
            VisibleLight visibleLight = vl;

            bool dirFlag = dirLightCount < MaxDirLightCount;
            bool puncFlag = puncLightCount < MaxPunctualLights;

            if (!dirFlag && !puncFlag) break;

            switch (visibleLight.lightType) {
                case LightType.Directional when dirFlag:
                    SetupDirectionalLight(dirLightCount++, i, ref visibleLight);
                    break;
                case LightType.Point when puncFlag:
                    SetupPointLight(puncLightCount++, i, ref visibleLight);
                    break;
                case LightType.Spot when puncFlag:
                    SetupSpotLight(puncLightCount++, i, ref visibleLight);
                    break;
                case LightType.Area:
                case LightType.Disc:
                default:
                    break;
            }
        }

        buffer.SetGlobalInt(_dirLightCountId, dirLightCount);
        buffer.SetGlobalInt(_punctualLightCountId, puncLightCount);

        if (dirLightCount > 0) {
            buffer.SetGlobalVectorArray(_dirLightColorsId, _dirLightColors);
            buffer.SetGlobalVectorArray(_dirLightDirectionsId, _dirLightDirections);
            buffer.SetGlobalVectorArray(_dirLightShadowDataId, _dirLightShadowData);
        }

        if (puncLightCount > 0) {
            buffer.SetGlobalVectorArray(_punctualLightColorsId, _punctualLightColors);
            buffer.SetGlobalVectorArray(_punctualLightPositionsId, _punctualLightPositions);
            buffer.SetGlobalVectorArray(_punctualLightDirectionsId, _punctualLightDirections);
            buffer.SetGlobalVectorArray(_punctualLightSpotAnglesId, _punctualLightSpotAngles);
            buffer.SetGlobalVectorArray(_punctualLightShadowDataId, _punctualLightShadowData);
        }
    }

    private void SetupDirectionalLight (int index, int visibleIndex, ref VisibleLight visibleLight) {
        _dirLightColors[index] = visibleLight.finalColor;
        _dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        _dirLightShadowData[index] = _shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
    }
    
    private void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight) {
        _punctualLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        _punctualLightPositions[index] = position;
        _punctualLightDirections[index] = Vector4.zero;
        
        Light light = visibleLight.light;
        _punctualLightSpotAngles[index] = new Vector4(0, 1);
        _punctualLightShadowData[index] = _shadows.ReservePunctualShadows(light, visibleIndex);
    }

    private void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight) {
        
        _punctualLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        _punctualLightPositions[index] = position;
        _punctualLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        
        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        _punctualLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        _punctualLightShadowData[index] = _shadows.ReservePunctualShadows(light, visibleIndex);
    }
    
    public void Cleanup () {
        _shadows.Cleanup();
    }
}