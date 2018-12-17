Shader "Hidden/NoiseRing"
{
	Properties
	{
	}
	SubShader
	{
		Cull Front
		Blend One One
		ZWrite Off
		Tags
		{
			"RenderType"="Transparent"
			"Queue"="Transparent"
		}

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "NoiseRingGlobals.cginc"
			#include "CGNoise/Noise.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 prevVertex : TEXCOORD0;
				float3 nextVertex : TEXCOORD1;
				float3 uvAndOrientation : TEXCOORD2;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _LineTexture;
			fixed4 _Color;
			float _Multiplier;
			float _LineWidth;
			float _Radius;
			float _Height;
			float _Intensity;
			float _NoiseScale;
			float _NoiseHeight;
			float _DetailNoiseScale;
			float _DetailNoiseHeight;
			float _Speed;
			float _NoiseTime;

			
			float3 applyNoise (float3 pos, float intensity)
			{
				float speedTime = _Speed * _NoiseTime;
				float bigTime = 100.0 * _NoiseTime; 
				float3 noisePos = pos;
				float noiseSample = 0.5 * snoise(float3(_NoiseScale * pos.xy, speedTime)) + 0.5;
				float detailNoiseSample = 0.5 * snoise(float3(_DetailNoiseScale * pos.xy, bigTime)) + 0.5;
				float sumNoise = _NoiseHeight * noiseSample + _DetailNoiseHeight * detailNoiseSample;
				noisePos.z = _Height + intensity * sumNoise;
				return noisePos;
			}

			v2f vert (appdata v)
			{
				float2 uv = v.uvAndOrientation.xy;
				float orientation = v.uvAndOrientation.z;

				float2 aspect = float2(_ScreenParams.x / _ScreenParams.y, 1.0);

				v.vertex.xy *= _Radius;
				v.prevVertex.xy *= _Radius;
				v.nextVertex.xy *= _Radius;

				float2 dirA = float2(0.0, 0.0);
				float2 dirB = float2(0.0, 0.0);
				float2 perp = float2(0.0, 0.0);

				float3 noisePos = applyNoise(v.vertex.xyz, _Intensity);
				float4 currClipPos = UnityObjectToClipPos(noisePos);
				float2 currScreenPos = currClipPos.xy / currClipPos.w * aspect;

				float3 prevNoisePos = applyNoise(v.prevVertex, _Intensity);
				float4 prevClipPos = UnityObjectToClipPos(prevNoisePos);
				float2 prevScreenPos = prevClipPos.xy / prevClipPos.w * aspect;
				dirA = normalize(currScreenPos - prevScreenPos);
				perp = float2(-dirA.y, dirA.x);

				float3 nextNoisePos = applyNoise(v.nextVertex, _Intensity);
				float4 nextClipPos = UnityObjectToClipPos(nextNoisePos);
				float2 nextScreenPos = nextClipPos.xy / nextClipPos.w * aspect;
				dirB = normalize(nextScreenPos - currScreenPos);

				float2 tangent = normalize(dirA + dirB);
				float2 miter = float2(-tangent.y, tangent.x);

				float4x4 clipToWorld = NoiseRingGlobals_ClipToWorldMatrix();
				float3 worldMiter = normalize(mul(clipToWorld, float4(miter / aspect, 0.0, 0.0)).xyz);
				float3 worldPerp = normalize(mul(clipToWorld, float4(perp / aspect, 0.0, 0.0)).xyz);

				float width = _LineWidth / dot(worldMiter, worldPerp);
				width = min(width, 1.5 * _LineWidth);
				width = max(width, 0.0);

				float3 worldPos = mul(unity_ObjectToWorld, float4(noisePos, 1.0)).xyz;
				worldPos += 0.5 * width * orientation * worldMiter;

				v2f o;
				o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
				o.uv = uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(_Color.rgb * tex2D(_LineTexture, i.uv) * _Multiplier, 0.0);
				return col;
			}
			ENDCG
		}
	}
}
