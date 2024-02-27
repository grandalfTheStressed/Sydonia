using System;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows {
	private const string BufferName = "Shadows";

	private const int MaxShadowedDirLightCount = 4;
	private const int MaxShadowedPunctualLightCount = 512;
	private const int MaxCascades = 4;

	private static readonly string[] DirectionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7"
	};

	private static readonly string[] PunctualFilterKeywords = {
		"_PUNCTUAL_PCF3",
		"_PUNCTUAL_PCF5",
		"_PUNCTUAL_PCF7"
	};

	private static readonly string[] CascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
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
	private static readonly int JitterId = Shader.PropertyToID("_Jitter");
	private static readonly int NoiseOffsetId = Shader.PropertyToID("_NoiseOffset");

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

	private int shadowedDirLightCount;
	private int shadowedPunctualLightCount;
	
	private readonly CommandBuffer buffer = new() {
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
				24, 
				FilterMode.Bilinear, 
				RenderTextureFormat.Shadowmap);
		} 
		if (shadowedPunctualLightCount > 0) {
			RenderPunctualShadows();
		}
		else {
			buffer.SetGlobalTexture(PunctualShadowAtlasId, DirShadowAtlasId);
		}
	
		buffer.SetGlobalInt(CascadeCountId, shadowedDirLightCount > 0 ? shadowSettings.directional.cascadeCount : 0);

		float f = 1f - shadowSettings.directional.cascadeFade;
		buffer.SetGlobalVector(ShadowDistanceFadeId, new Vector4(
			1f / shadowSettings.maxDistance, 
			1f / shadowSettings.distanceFade,
			1f / (1f - f * f)));
		
		buffer.SetGlobalVector(ShadowAtlasSizeId, atlasSizes);
		buffer.SetGlobalFloat(JitterId, shadowSettings.jitter);
		buffer.SetGlobalFloat(NoiseOffsetId, shadowSettings.noiseOffset);
		
		ExecuteBuffer();
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
			24, 
			FilterMode.Bilinear,
			RenderTextureFormat.Shadowmap);
		buffer.SetRenderTarget(DirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		buffer.ClearRenderTarget(true, false, Color.clear);
		buffer.SetGlobalFloat(ShadowPancakingId, 1f);
		buffer.BeginSample(BufferName);
		ExecuteBuffer();
		
		int tiles = shadowedDirLightCount * shadowSettings.directional.cascadeCount;
		int split = (int)Math.Ceiling(Math.Sqrt(tiles));
		int tileSize = atlasSize / split;
		
		for (int i = 0; i < shadowedDirLightCount; i++)
		{
			RenderDirectionalShadow(i, split, tileSize);
		}

		buffer.SetGlobalVector(ShadowDistanceFadeId, new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade));
		buffer.SetGlobalVectorArray(CascadeCullingSpheresId, CascadeCullingSpheres);
		buffer.SetGlobalVectorArray(CascadeDataId, CascadeData);
		buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);
		SetKeywords(DirectionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
		SetKeywords(CascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);
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
	
	public Vector4 ReservePunctualShadows (Light light, int visibleLightIndex) {
		bool isPoint = light.type == LightType.Point;
		int newLightCount = shadowedPunctualLightCount + (isPoint ? 6 : 1);
		if (light.shadows == LightShadows.None || 
		    light.shadowStrength <= 0f || 
		    newLightCount > MaxShadowedPunctualLightCount ||
		    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
			return new Vector4(0f, 0f, 0f, -1f);
		}

		shadowedPunctualLights[shadowedPunctualLightCount] = new ShadowedPunctualLight {
			VisibleLightIndex = visibleLightIndex,
			SlopeScaleBias = light.shadowBias,
			NormalBias = light.shadowNormalBias,
			IsPoint = isPoint
		};

		Vector4 data = new Vector4(light.shadowStrength, shadowedPunctualLightCount, isPoint ? 1f : 0f, -1);
		shadowedPunctualLightCount = newLightCount;

		return data;
	}
	
	private void RenderPunctualShadows()
	{
		int atlasSize = (int)shadowSettings.punctual.atlasSize;
		atlasSizes.z = atlasSize;
		atlasSizes.w = 1f / atlasSize;
		buffer.GetTemporaryRT(PunctualShadowAtlasId, 
			atlasSize, atlasSize, 
			32, 
			FilterMode.Bilinear,
			RenderTextureFormat.Shadowmap);
		buffer.SetRenderTarget(PunctualShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		buffer.ClearRenderTarget(true, false, Color.clear);
		buffer.SetGlobalFloat(ShadowPancakingId, 0f);
		buffer.BeginSample(BufferName);
		ExecuteBuffer();
		
		int tiles = shadowedPunctualLightCount;
		int split = (int)Math.Ceiling(Math.Sqrt(tiles));
		int tileSize = atlasSize / split;

		for (int i = 0; i < shadowedPunctualLightCount;)
		{
			if (shadowedPunctualLights[i].IsPoint) {
				RenderPointShadow(i, split, tileSize);
				i += 6;
			}
			else {
				RenderSpotShadow(i, split, tileSize);
				i += 1;
			}
		}

		buffer.SetGlobalMatrixArray(PunctualShadowMatricesId, PunctualShadowMatrices);
		buffer.SetGlobalVectorArray(PunctualShadowTilesId, PunctualShadowTiles);
		SetKeywords(PunctualFilterKeywords, (int)shadowSettings.punctual.filter - 1);
		buffer.EndSample(BufferName);
		ExecuteBuffer();
	}
	
	void RenderSpotShadow(int index, int split, int tileSize) {
		ShadowedPunctualLight light = shadowedPunctualLights[index];
		ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(
			cullingResults, light.VisibleLightIndex,
			BatchCullingProjectionType.Perspective
		);
		cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
			light.VisibleLightIndex, 
			out Matrix4x4 viewMatrix,
			out Matrix4x4 projectionMatrix, 
			out ShadowSplitData splitData
		);
		shadowDrawingSettings.splitData = splitData;
		float texelSize = 2f / (tileSize * projectionMatrix.m00);
		float filterSize = texelSize * ((float)shadowSettings.punctual.filter + 1f);
		float bias = light.NormalBias * filterSize * 1.4142136f;
		Vector2 offset = SetTileViewport(index, split, tileSize);
		float tileScale = 1f / split;
		SetPunctualTileData(index, offset,tileScale, bias);
		
		PunctualShadowMatrices[index] = ConvertToAtlasMatrix(
			projectionMatrix * viewMatrix,
			offset, 
			tileScale
		);
		buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
		buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
		ExecuteBuffer();
		context.DrawShadows(ref shadowDrawingSettings);
		buffer.SetGlobalDepthBias(0f, 0f);
	}
	
	void RenderPointShadow (int index, int split, int tileSize) {
		ShadowedPunctualLight light = shadowedPunctualLights[index];
		ShadowDrawingSettings shadowDrawingSettings= new ShadowDrawingSettings(
			cullingResults, light.VisibleLightIndex,
			BatchCullingProjectionType.Perspective
		);
		
		float texelSize = 2f / tileSize;
		float filterSize = texelSize * ((float)shadowSettings.punctual.filter + 1f);
		float bias = light.NormalBias * filterSize * 1.4142136f;
		float tileScale = 1f / split;
		float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
		
		for (int i = 0; i < 6; i++) {
			cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
				light.VisibleLightIndex, 
				(CubemapFace)i, 
				fovBias,
				out Matrix4x4 viewMatrix, 
				out Matrix4x4 projectionMatrix,
				out ShadowSplitData splitData
			);
			
			shadowDrawingSettings.splitData = splitData;
			int tileIndex = index + i;
			Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
			SetPunctualTileData(tileIndex, offset, tileScale, bias);
			PunctualShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
				projectionMatrix * viewMatrix, 
				offset, 
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
