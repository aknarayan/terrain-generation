Shader "Custom/Terrain"
{
  Properties
  {
	testTexture("Texture", 2D) = "white"{}
	testScale("Scale", Float) = 1
  }
  SubShader
  {
	  Tags { "RenderType" = "Opaque" }
	  LOD 200

	  CGPROGRAM
	// Physically based Standard lighting model, and enable shadows on all light types
	#pragma surface surf Standard fullforwardshadows

	// Use shader model 3.0 target, to get nicer looking lighting
	#pragma target 3.0

	const static int maxColourCount = 8;
    const static float epsilon = 1E-4;  

	int baseColourCount;
	float3 baseColours[maxColourCount];
	float baseStartHeights[maxColourCount];
	float baseBlends[maxColourCount];

    float minHeight;
	float maxHeight;

	sampler2D testTexture;
	float testScale;

    struct Input
	{
	  float3 worldPos;
	  float3 worldNormal;
	};

	// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
	// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
	// #pragma instancing_options assumeuniformscaling
	UNITY_INSTANCING_BUFFER_START(Props)
	  // put more per-instance properties here
	UNITY_INSTANCING_BUFFER_END(Props)

	float inverseLerp(float a, float b, float value) {
	  return saturate((value - a) / (b - a));
	}

	void surf(Input IN, inout SurfaceOutputStandard o) {
	  float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
	  for (int i = 0; i < baseColourCount; i++) {
		float drawStrength = inverseLerp(- baseBlends[i] / 2 - epsilon, baseBlends[i] / 2, heightPercent - baseStartHeights[i]);
		o.Albedo = o.Albedo * (1 - drawStrength) + baseColours[i] * drawStrength;
	  }

	  float3 scaledWorldPos = IN.worldPos / testScale;
	  float3 blendAxes = abs(IN.worldNormal);
	  blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
	  float3 xProjection = tex2D(testTexture, scaledWorldPos.yz) * blendAxes.x;
	  float3 yProjection = tex2D(testTexture, scaledWorldPos.xz) * blendAxes.y;
	  float3 zProjection = tex2D(testTexture, scaledWorldPos.xy) * blendAxes.z;

	  o.Albedo = xProjection + yProjection + zProjection;

	}
	ENDCG
  }
	FallBack "Diffuse"
}
