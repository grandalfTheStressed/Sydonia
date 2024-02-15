using UnityEngine;
using UnityEngine.Rendering;

public class Shadows {
	private const string BufferName = "Shadows";

	private const int MaxShadowedDirLightCount = 4, MaxShadowedPunctualLightCount = 16;
	private const int MaxCascades = 4;

	private static readonly string[] DirectionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7"
	};

	private static readonly string[] PunctualFilterKeywords = {
		"_Punctual_PCF3",
		"_Punctual_PCF5",
		"_Punctual_PCF7"
	};

	private static readonly string[] CascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
	};

	private static readonly string[] ShadowMaskKeywords = {
		"_SHADOW_MASK_ALWAYS",
		"_SHADOW_MASK_DISTANCE"
	};

	private static readonly int DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
	private static readonly int DirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
	private static readonly int PunctualShadowAtlasId = Shader.PropertyToID("_PunctualShadowAtlas");
	private static readonly int PunctualShadowMatricesId = Shader.PropertyToID("_PunctualShadowMatrices");
	private static readonly int PunctualShadowTilesId = Shader.PropertyToID("_PunctualShadowTiles");
	private static readonly int CascadeCountId = Shader.PropertyToID("_CascadeCount");
	private static readonly int CascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
	private static readonly int CascadeDataId = Shader.PropertyToID("_CascadeData");
	private static readonly int ShadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
	private static readonly int ShadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
	private static readonly int ShadowPancakingId = Shader.PropertyToID("_ShadowPancaking");

	private static readonly Vector4[] CascadeCullingSpheres = new Vector4[MaxCascades];
	private static readonly Vector4[] CascadeData = new Vector4[MaxCascades];
	private static readonly Vector4[] PunctualShadowTiles = new Vector4[MaxShadowedPunctualLightCount];

	private static readonly Matrix4x4[] DirShadowMatrices = new Matrix4x4[MaxShadowedDirLightCount * MaxCascades];
	private static readonly Matrix4x4[] PunctualShadowMatrices = new Matrix4x4[MaxShadowedPunctualLightCount];

	private struct ShadowedDirectionalLight
	{
		public int VisibleLightIndex;
		public float SlopeScaleBias;
		public float NearPlaneOffset;
	}
	
	private struct ShadowedPunctualLight
	{
		public int VisibleLightIndex;
		public float SlopeScaleBias;
		public float NormalBias;
		public bool IsPoint;
	}

	private readonly ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirLightCount];
	private readonly ShadowedPunctualLight[] shadowedPunctualLights = new ShadowedPunctualLight[MaxShadowedPunctualLightCount];

	private int shadowedDirLightCount, shadowedPunctualLightCount;

	private readonly CommandBuffer buffer = new()
	{
		name = BufferName
	};

	private ScriptableRenderContext context;

	private CullingResults cullingResults;

	private ShadowSettings shadowSettings;

	private Vector4 atlasSizes;

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings) {
		this.context = context;
		this.cullingResults = cullingResults;
		this.shadowSettings = shadowSettings;
		shadowedDirLightCount = shadowedPunctualLightCount = 0;
	}

	public void Cleanup()
	{
		buffer.ReleaseTemporaryRT(DirShadowAtlasId);
		if (shadowedPunctualLightCount > 0) {
			buffer.ReleaseTemporaryRT(PunctualShadowAtlasId);
		}
		ExecuteBuffer();
	}

	public void Render()
	{
		if (shadowedDirLightCount > 0) {
			RenderDirectionalShadows();
		} else {
			buffer.GetTemporaryRT(
				DirShadowAtlasId, 
				1, 
				1,
				32, 
				FilterMode.Bilinear, 
				RenderTextureFormat.Shadowmap);
		} 
	}

	public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
	{
		if (shadowedDirLightCount >= MaxShadowedDirLightCount || 
		    light.shadows == LightShadows.None || 
		    !(light.shadowStrength > 0f) || 
		    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) 
			return new Vector4(0f, 0f, 0f, -1f);
		
		shadowedDirectionalLights[shadowedDirLightCount] = new ShadowedDirectionalLight {
			VisibleLightIndex = visibleLightIndex,
			SlopeScaleBias = light.shadowBias,
			NearPlaneOffset = light.shadowNearPlane
		};
		
		return new Vector4(
			light.shadowStrength,
			shadowSettings.directional.cascadeCount * shadowedDirLightCount++,
			light.shadowNormalBias, 
			0);
	}
	
	private void RenderDirectionalShadows()
	{
		int atlasSize = (int)shadowSettings.directional.atlasSize;
		atlasSizes.x = atlasSize;
		atlasSizes.y = 1f / atlasSize;
		buffer.GetTemporaryRT(DirShadowAtlasId, 
			atlasSize, atlasSize, 
			32, 
			FilterMode.Bilinear,
			RenderTextureFormat.Shadowmap);
		buffer.SetRenderTarget(DirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		buffer.ClearRenderTarget(true, false, Color.clear);
		buffer.BeginSample(BufferName);
		ExecuteBuffer();

		int tiles = shadowedDirLightCount * shadowSettings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;

		for (int i = 0; i < shadowedDirLightCount; i++)
		{
			RenderDirectionalShadow(i, split, tileSize);
		}

		buffer.SetGlobalVector(ShadowDistanceFadeId, new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade));
		buffer.SetGlobalVectorArray(CascadeCullingSpheresId, CascadeCullingSpheres);
		buffer.SetGlobalVectorArray(CascadeDataId, CascadeData);
		buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);
		buffer.BeginSample(BufferName);
		buffer.SetGlobalInt(CascadeCountId, shadowedDirLightCount > 0 ? shadowSettings.directional.cascadeCount : 0);
		SetKeywords(DirectionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
		SetKeywords(CascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);
		buffer.EndSample(BufferName);
		
		float f = 1f - shadowSettings.directional.cascadeFade;
		buffer.SetGlobalVector(ShadowDistanceFadeId, new Vector4(
			1f / shadowSettings.maxDistance, 
			1f / shadowSettings.distanceFade,
			1f / (1f - f * f)));
		buffer.SetGlobalVector(ShadowAtlasSizeId, atlasSizes);
		buffer.EndSample(BufferName);
		ExecuteBuffer();
	}

	private void RenderDirectionalShadow(int index, int split, int tileSize)
	{
		ShadowedDirectionalLight light = shadowedDirectionalLights[index];
		ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(
			cullingResults, 
			light.VisibleLightIndex, 
			BatchCullingProjectionType.Orthographic);
		
		int cascadeCount = shadowSettings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = shadowSettings.directional.CascadeRatios;
		float cullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.directional.cascadeFade);
		float tileScale = 1f / split;
		
		for (int i = 0; i < cascadeCount; i++)
		{
			cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
				light.VisibleLightIndex, 
				i, 
				cascadeCount, 
				ratios, 
				tileSize,
				light.NearPlaneOffset, 
				out Matrix4x4 viewMatrix,
				out Matrix4x4 projectionMatrix,
				out ShadowSplitData splitData);
			
			splitData.shadowCascadeBlendCullingFactor = cullingFactor;
			shadowDrawingSettings.splitData = splitData;
			if (index == 0) {
				SetCascadeData(i, splitData.cullingSphere, tileSize);
			}
			int tileIndex = tileOffset + i;
			DirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
				projectionMatrix * viewMatrix, 
				SetTileViewport(tileIndex, split, tileSize), 
				tileScale);
			buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
			buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
			ExecuteBuffer();
			context.DrawShadows(ref shadowDrawingSettings);
			buffer.SetGlobalDepthBias(0f, 0f);
		}
	}

	private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
	{
		float texelSize = 2f * cullingSphere.w / tileSize;
		float filterSize = texelSize * ((float)shadowSettings.directional.filter + 1f);
		cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		CascadeCullingSpheres[index] = cullingSphere;
		CascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
	}

	private void SetPunctualTileData(int index, Vector2 offset, float scale, float bias)
	{
		float border = atlasSizes.w * 0.5f;
		Vector4 data;
		data.x = offset.x * scale + border;
		data.y = offset.y * scale + border;
		data.z = scale - border - border;
		data.w = bias;
		PunctualShadowTiles[index] = data;
	}

	private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale) {
		if (SystemInfo.usesReversedZBuffer)
		{
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
		return m;
	}

	private Vector2 SetTileViewport (int index, int split, float tileSize) {
		var offset = new Vector2(index % split, index / split);
		buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
		return offset;
	}

	private void SetKeywords(string[] keywords, int enabledIndex) {
		for (int i = 0; i < keywords.Length; i++)
		{
			if (i == enabledIndex)
			{
				buffer.EnableShaderKeyword(keywords[i]);
			}
			else
			{
				buffer.DisableShaderKeyword(keywords[i]);
			}
		}
	}

	private void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}
