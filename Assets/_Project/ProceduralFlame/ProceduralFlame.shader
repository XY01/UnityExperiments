// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "ProceduralFlame"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[ASEBegin]_Alpha("Alpha", Range( 0 , 5)) = 1.748938
		_LocalWindSpeed("LocalWindSpeed", Range( 0 , 1.5)) = 1.572329
		_Texture0("Texture 0", 2D) = "white" {}
		[Header(Low Freq)]_FreqLow("FreqLow", Range( 0 , 3)) = 0.3
		_NoiseLowFreqStr("Noise Low Freq Str", Range( 0 , 1)) = 1
		_NoiseLowFreqExp("Noise Low Freq Exp", Range( 0 , 4)) = 1
		[Header(Freq High)]_FreqHigh("FreqHigh", Range( 0 , 3)) = 1.755391
		_NoiseHighFreqStr("Noise High Freq Str", Range( 0 , 1)) = 1
		_NoiseHighFreqExp("Noise High Freq Exp", Range( 0 , 4)) = 1
		[Header(Stretch scale)]_Stretch("Stretch", Range( 0 , 4)) = 1
		_ObjectscaledLerp("Object scaled Lerp", Range( 0 , 1)) = 0
		[ASEEnd]_VertMaskExp("VertMaskExp", Range( 0 , 8)) = 4

		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0

		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
		
		Cull Back
		AlphaToMask Off
		
		HLSLINCLUDE
		#pragma target 2.0

		#pragma prefer_hlslcc gles
		#pragma exclude_renderers d3d11_9x 

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}
		
		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS

		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			
			#pragma multi_compile_instancing
			#define _RECEIVE_SHADOWS_OFF 1
			#define ASE_SRP_VERSION 999999

			
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _LocalWindSpeed;
			float _Stretch;
			float _ObjectscaledLerp;
			float _FreqLow;
			float _NoiseLowFreqExp;
			float _NoiseLowFreqStr;
			float _FreqHigh;
			float _NoiseHighFreqExp;
			float _NoiseHighFreqStr;
			float _VertMaskExp;
			float _Alpha;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Texture0;


			
			float4 SampleGradient( Gradient gradient, float time )
			{
				float3 color = gradient.colors[0].rgb;
				UNITY_UNROLL
				for (int c = 1; c < 8; c++)
				{
				float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, gradient.colorsLength-1));
				color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
				}
				#ifndef UNITY_COLORSPACE_GAMMA
				color = SRGBToLinear(color);
				#endif
				float alpha = gradient.alphas[0].x;
				UNITY_UNROLL
				for (int a = 1; a < 8; a++)
				{
				float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, gradient.alphasLength-1));
				alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
				}
				return float4(color, alpha);
			}
			
			
			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif
				Gradient gradient489 = NewGradient( 0, 5, 2, float4( 0.3960785, 0.07843138, 0, 0 ), float4( 0.8431373, 0.2509804, 0, 0.197055 ), float4( 0.9803922, 0.4156863, 0, 0.4088197 ), float4( 0.9960785, 0.4862745, 0.007843138, 0.6470588 ), float4( 0.9921569, 0.9921569, 0.6078432, 1 ), 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
				float2 texCoord132 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( -0.5,0 );
				float3 objToWorld115 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				float RandFromPos128 = ( objToWorld115.x + objToWorld115.y );
				float2 appendResult388 = (float2(texCoord132.x , ( ( 1.0 - texCoord132.y ) + RandFromPos128 )));
				float2 break145 = appendResult388;
				float WindOffset126 = ( ( _TimeParameters.x ) * _LocalWindSpeed );
				float2 appendResult146 = (float2(break145.x , ( break145.y + WindOffset126 )));
				float2 WindOffsetUV270 = appendResult146;
				float2 break390 = WindOffsetUV270;
				float temp_output_381_0 = ( 0.07 * sin( ( break390.y * 3.0 ) ) );
				float2 texCoord405 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult395 = (float2(( abs( ( break390.x + temp_output_381_0 ) ) * 2.0 ) , texCoord405.y));
				float2 sinUV384 = appendResult395;
				float2 texCoord460 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( -0.5,0 );
				float2 appendResult459 = (float2(sinUV384.x , pow( texCoord460.y , 4.0 )));
				float2 appendResult316 = (float2(1.0 , _Stretch));
				float2 temp_output_310_0 = ( WindOffsetUV270 * appendResult316 );
				float2 break344 = temp_output_310_0;
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float clampResult347 = clamp( ase_objectScale.x , 0.0 , 100.0 );
				float clampResult319 = clamp( ase_objectScale.y , 0.0 , 100.0 );
				float2 appendResult348 = (float2(( break344.x * clampResult347 ) , ( break344.y * clampResult319 )));
				float2 lerpResult312 = lerp( temp_output_310_0 , appendResult348 , _ObjectscaledLerp);
				float2 ObjectScaledUVs365 = lerpResult312;
				float2 break470 = ObjectScaledUVs365;
				float scrollingVSin474 = temp_output_381_0;
				float2 appendResult473 = (float2(( break470.x + scrollingVSin474 ) , break470.y));
				float NoiseLowFreq263 = ( 1.0 - tex2D( _Texture0, ( appendResult473 * _FreqLow ) ).r );
				float temp_output_280_0 = ( pow( NoiseLowFreq263 , _NoiseLowFreqExp ) * _NoiseLowFreqStr );
				float HF_VOffset503 = ( 1.0 * sin( ( WindOffsetUV270.y * 2.0 ) ) );
				float2 appendResult477 = (float2(( break470.x + ( 1.0 - scrollingVSin474 ) ) , HF_VOffset503));
				float NoiseHighFreq262 = ( 1.0 - tex2D( _Texture0, ( appendResult477 * _FreqHigh ) ).r );
				float temp_output_293_0 = ( pow( NoiseHighFreq262 , _NoiseHighFreqExp ) * _NoiseHighFreqStr );
				float CombinedNoise356 = saturate( ( temp_output_280_0 - temp_output_293_0 ) );
				float2 texCoord444 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float VerticalMask445 = pow( ( 1.0 - texCoord444.y ) , _VertMaskExp );
				float temp_output_438_0 = length( ( appendResult459 + ( CombinedNoise356 * ( 1.0 - VerticalMask445 ) * 1.5 ) ) );
				float smoothstepResult493 = smoothstep( 1.0 , 0.0 , temp_output_438_0);
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = SampleGradient( gradient489, smoothstepResult493 ).rgb;
				float Alpha = saturate( ( smoothstepResult493 * _Alpha ) );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM
			
			#pragma multi_compile_instancing
			#define _RECEIVE_SHADOWS_OFF 1
			#define ASE_SRP_VERSION 999999

			
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _LocalWindSpeed;
			float _Stretch;
			float _ObjectscaledLerp;
			float _FreqLow;
			float _NoiseLowFreqExp;
			float _NoiseLowFreqStr;
			float _FreqHigh;
			float _NoiseHighFreqExp;
			float _NoiseHighFreqStr;
			float _VertMaskExp;
			float _Alpha;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Texture0;


			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.clipPos = TransformWorldToHClip( positionWS );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = o.clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 texCoord132 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( -0.5,0 );
				float3 objToWorld115 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				float RandFromPos128 = ( objToWorld115.x + objToWorld115.y );
				float2 appendResult388 = (float2(texCoord132.x , ( ( 1.0 - texCoord132.y ) + RandFromPos128 )));
				float2 break145 = appendResult388;
				float WindOffset126 = ( ( _TimeParameters.x ) * _LocalWindSpeed );
				float2 appendResult146 = (float2(break145.x , ( break145.y + WindOffset126 )));
				float2 WindOffsetUV270 = appendResult146;
				float2 break390 = WindOffsetUV270;
				float temp_output_381_0 = ( 0.07 * sin( ( break390.y * 3.0 ) ) );
				float2 texCoord405 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult395 = (float2(( abs( ( break390.x + temp_output_381_0 ) ) * 2.0 ) , texCoord405.y));
				float2 sinUV384 = appendResult395;
				float2 texCoord460 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( -0.5,0 );
				float2 appendResult459 = (float2(sinUV384.x , pow( texCoord460.y , 4.0 )));
				float2 appendResult316 = (float2(1.0 , _Stretch));
				float2 temp_output_310_0 = ( WindOffsetUV270 * appendResult316 );
				float2 break344 = temp_output_310_0;
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float clampResult347 = clamp( ase_objectScale.x , 0.0 , 100.0 );
				float clampResult319 = clamp( ase_objectScale.y , 0.0 , 100.0 );
				float2 appendResult348 = (float2(( break344.x * clampResult347 ) , ( break344.y * clampResult319 )));
				float2 lerpResult312 = lerp( temp_output_310_0 , appendResult348 , _ObjectscaledLerp);
				float2 ObjectScaledUVs365 = lerpResult312;
				float2 break470 = ObjectScaledUVs365;
				float scrollingVSin474 = temp_output_381_0;
				float2 appendResult473 = (float2(( break470.x + scrollingVSin474 ) , break470.y));
				float NoiseLowFreq263 = ( 1.0 - tex2D( _Texture0, ( appendResult473 * _FreqLow ) ).r );
				float temp_output_280_0 = ( pow( NoiseLowFreq263 , _NoiseLowFreqExp ) * _NoiseLowFreqStr );
				float HF_VOffset503 = ( 1.0 * sin( ( WindOffsetUV270.y * 2.0 ) ) );
				float2 appendResult477 = (float2(( break470.x + ( 1.0 - scrollingVSin474 ) ) , HF_VOffset503));
				float NoiseHighFreq262 = ( 1.0 - tex2D( _Texture0, ( appendResult477 * _FreqHigh ) ).r );
				float temp_output_293_0 = ( pow( NoiseHighFreq262 , _NoiseHighFreqExp ) * _NoiseHighFreqStr );
				float CombinedNoise356 = saturate( ( temp_output_280_0 - temp_output_293_0 ) );
				float2 texCoord444 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float VerticalMask445 = pow( ( 1.0 - texCoord444.y ) , _VertMaskExp );
				float temp_output_438_0 = length( ( appendResult459 + ( CombinedNoise356 * ( 1.0 - VerticalMask445 ) * 1.5 ) ) );
				float smoothstepResult493 = smoothstep( 1.0 , 0.0 , temp_output_438_0);
				
				float Alpha = saturate( ( smoothstepResult493 * _Alpha ) );
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

	
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=18935
0;73;1604;756;6901.686;-19.57443;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;118;-6660.151,-1596.861;Inherit;False;582.1733;238;rand offset;3;128;116;115;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TransformPositionNode;115;-6631.151,-1549.861;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;197;-6661.867,-1331.598;Inherit;False;656.6177;253.1786;Offset UVS with random offset;5;132;131;388;129;392;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;116;-6416.625,-1521.201;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;132;-6647.403,-1284.613;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;-0.5,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;128;-6282.463,-1522.399;Inherit;False;RandFromPos;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;129;-6641.582,-1162.014;Inherit;False;128;RandFromPos;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;392;-6434.357,-1181.967;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;130;-6659.936,-1058.57;Inherit;False;843.3767;394.2556;Wind - getting weird errors when going into negate, hence subtraction from large float;5;179;171;126;125;124;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;124;-6525.295,-770.7726;Inherit;False;Property;_LocalWindSpeed;LocalWindSpeed;1;0;Create;True;0;0;0;False;0;False;1.572329;0.85;0;1.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;131;-6287.463,-1181.459;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;179;-6445.344,-915.178;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;388;-6154.856,-1258.858;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;149;-5789.67,-1052.329;Inherit;False;728.0845;237.7458;UV wind offset;4;175;145;146;270;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;-6196.754,-894.8102;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;126;-6034.194,-899.314;Inherit;False;WindOffset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;145;-5662.066,-1009.245;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;175;-5542.686,-923.8701;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;146;-5425.23,-1010.797;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;469;-5778.795,-801.5792;Inherit;False;1527.292;615.5494;Object scaled UVs;14;313;348;315;314;271;310;347;346;344;312;319;365;316;318;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;270;-5283.587,-1005.665;Inherit;False;WindOffsetUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;318;-5728.795,-557.7411;Inherit;False;Property;_Stretch;Stretch;11;1;[Header];Create;True;1;Stretch scale;0;0;False;0;False;1;0.3;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;316;-5431.795,-579.7406;Inherit;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;271;-5488.78,-663.641;Inherit;False;270;WindOffsetUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;382;-6620.128,-164.8927;Inherit;False;2055.21;454.9011;Sin wave UV;14;384;395;403;369;400;374;381;371;379;380;389;390;405;474;;1,1,1,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;389;-6592.853,-76.83468;Inherit;False;270;WindOffsetUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;310;-5236.932,-659.91;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ObjectScaleNode;314;-5391.401,-455.0298;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ClampOpNode;319;-5208.794,-413.0263;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;380;-6181.457,42.85618;Inherit;False;Constant;_Freq;Freq;15;0;Create;True;0;0;0;False;0;False;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;347;-5204.435,-535.5796;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;390;-6374.157,-80.60519;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.BreakToComponentsNode;344;-5092.435,-643.5792;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;496;-6459.488,424.6951;Inherit;False;270;WindOffsetUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;346;-4973.435,-751.5792;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;379;-6043.119,-14.63091;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;313;-4992.402,-532.0295;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;498;-6253.436,517.8931;Inherit;False;Constant;_FraqHF;FraqHF;15;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;315;-5229.401,-302.0298;Inherit;False;Property;_ObjectscaledLerp;Object scaled Lerp;12;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;497;-6240.792,420.9246;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DynamicAppendNode;348;-4859.435,-630.5792;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SinOpNode;374;-5903.914,64.83181;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;400;-5907.154,-21.09962;Inherit;False;Constant;_Amp;Amp;15;0;Create;True;0;0;0;False;0;False;0.07;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;499;-6115.098,460.406;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;312;-4659.457,-658.4037;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;381;-5755.158,-18.09301;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;474;-5631.134,220.232;Inherit;False;scrollingVSin;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;500;-5975.893,539.8688;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;501;-5979.132,453.9373;Inherit;False;Constant;_AmpHF;AmpHF;15;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;365;-4475.504,-662.7828;Inherit;False;ObjectScaledUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;467;-4532.776,-1285.331;Inherit;False;365;ObjectScaledUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;465;-4488.132,-1117.724;Inherit;False;474;scrollingVSin;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;502;-5827.137,456.9439;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;470;-4316.105,-1285.637;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.OneMinusNode;475;-4299.174,-1043.755;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;503;-5668.14,451.8098;Inherit;True;HF_VOffset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;504;-4110.945,-696.2338;Inherit;False;503;HF_VOffset;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;472;-4137.105,-1279.637;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;476;-4113.739,-1090.646;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;273;-3669.712,-706.5162;Inherit;False;1367.902;334.805;Freq High;6;262;247;248;250;257;466;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;272;-3824.187,-1321.639;Inherit;False;1494.838;332.5445;Freq Low;5;241;240;239;258;263;;1,1,1,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;477;-3907.662,-886.144;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;247;-3646.712,-519.6598;Inherit;False;Property;_FreqHigh;FreqHigh;8;1;[Header];Create;True;1;Freq High;0;0;False;0;False;1.755391;0.966;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;473;-4013.027,-1183.134;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;241;-3774.187,-1196.103;Inherit;False;Property;_FreqLow;FreqLow;5;1;[Header];Create;True;1;Low Freq;0;0;False;0;False;0.3;1.112;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;248;-3421.259,-643.0525;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;240;-3466.644,-1240.334;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;253;-3536.757,-963.548;Inherit;True;Property;_Texture0;Texture 0;4;0;Create;True;0;0;0;False;0;False;None;bb911711ee9d6694e892feafd898faca;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;250;-3115.259,-656.5164;Inherit;True;Property;_TextureSample1;Texture Sample 1;7;0;Create;True;0;0;0;False;0;False;-1;None;fcbb22463ab66e3409012b2ef95a3ff0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;239;-3144.993,-1271.639;Inherit;True;Property;_TextureSample0;Texture Sample 0;7;0;Create;True;0;0;0;False;0;False;-1;None;fcbb22463ab66e3409012b2ef95a3ff0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;258;-2826.171,-1243.094;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;257;-2750.046,-625.7114;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;299;-1769.143,-406.0931;Inherit;False;708.162;422.0067;Low Freq;5;282;280;267;285;264;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;263;-2553.348,-1245.457;Inherit;False;NoiseLowFreq;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;262;-2525.81,-626.8901;Inherit;False;NoiseHighFreq;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;298;-1788.166,123.2042;Inherit;False;729.2173;426.7254;High freq;5;293;289;290;295;291;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;290;-1748.834,369.0432;Inherit;False;Property;_NoiseHighFreqExp;Noise High Freq Exp;10;0;Create;True;0;0;0;False;0;False;1;1;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;285;-1733.17,-161.4335;Inherit;False;Property;_NoiseLowFreqExp;Noise Low Freq Exp;7;0;Create;True;0;0;0;False;0;False;1;1;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;291;-1724.372,177.3725;Inherit;True;262;NoiseHighFreq;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;264;-1714.608,-349.5644;Inherit;True;263;NoiseLowFreq;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;371;-5616.752,-93.46461;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;450;479.5235,-1490.324;Inherit;False;793;341;Vertical mask;5;445;449;446;444;456;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;444;529.5233,-1440.324;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;289;-1751.143,442.3677;Inherit;False;Property;_NoiseHighFreqStr;Noise High Freq Str;9;0;Create;True;0;0;0;False;0;False;1;0.441;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;267;-1497.527,-343.7256;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;4.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;369;-5417.386,-93.57336;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;282;-1730.941,-84.56928;Inherit;False;Property;_NoiseLowFreqStr;Noise Low Freq Str;6;0;Create;True;0;0;0;False;0;False;1;0.884;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;295;-1481.339,185.5712;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;4.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;403;-5247.319,-97.62488;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;456;614.2169,-1309.414;Inherit;False;Property;_VertMaskExp;VertMaskExp;15;0;Create;True;0;0;0;False;0;False;4;1.6;0;8;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;405;-5384.151,132.1246;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;280;-1334.082,-343.8111;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;446;747.5233,-1406.324;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;293;-1328.508,186.6652;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;349;-776.0164,-653.163;Inherit;False;997.1719;905.9513;Noise combine;10;2;4;3;0;260;355;268;360;361;362;;1,1,1,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;395;-4943.907,74.34101;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;362;-493.4987,-24.97697;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;449;896.5233,-1406.324;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;5.85;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;384;-4741.817,67.22647;Inherit;False;sinUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;268;-96.29477,-8.359677;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;462;426.5205,-626.1403;Inherit;False;1321.329;468.3773;Flame shape;12;439;457;433;438;459;452;441;432;437;461;451;460;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;445;1048.523,-1408.324;Inherit;True;VerticalMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;460;460.008,-479.265;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;-0.5,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;451;555.9808,-249.7006;Inherit;False;445;VerticalMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;356;247.3549,-3.840376;Inherit;False;CombinedNoise;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;437;504.6188,-571.129;Inherit;False;384;sinUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;495;765.0838,-117.9136;Inherit;False;Constant;_Float2;Float 2;16;0;Create;True;0;0;0;False;0;False;1.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;461;671.0714,-481.7771;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;432;691.7624,-330.8075;Inherit;False;356;CombinedNoise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;457;674.6912,-576.1403;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.OneMinusNode;452;756.1666,-248.6093;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;441;922.4126,-282.5737;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0.49;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;459;847.8115,-522.1667;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;433;1097.039,-528.521;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;305;1620.091,139.7141;Inherit;False;754.7336;308.509;Alpha;4;302;303;62;357;;1,1,1,1;0;0
Node;AmplifyShaderEditor.LengthOpNode;438;1315.202,-526.7167;Inherit;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;493;1785.212,-512.7949;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;62;1640.201,285.347;Inherit;False;Property;_Alpha;Alpha;0;0;Create;True;0;0;0;False;0;False;1.748938;1.07;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;195;552.9721,791.0896;Inherit;False;545.4974;189.2081;Debug;3;168;166;165;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;91;-601.1868,-2190.096;Inherit;False;1449.608;520.5379;Aledo;9;93;60;59;95;103;105;107;108;484;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;350;684.5469,-1115.853;Inherit;False;607.7307;291.8914;Colour;3;358;300;301;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;302;1972.019,190.7203;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;32;-3183.271,-2528.578;Inherit;False;1588.584;322.8889;Base Shape;7;31;208;30;29;23;24;225;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;277;-2271.882,-711.3768;Inherit;False;841.5389;234.1859;Intensity gradient from V;4;275;274;278;281;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;207;-1994.026,-1896.329;Inherit;False;665.5499;470.8324;Noise effect up the Y gradient;4;56;206;205;55;;1,1,1,1;0;0
Node;AmplifyShaderEditor.PowerNode;205;-1711.265,-1697.497;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;59;318.8258,-2141.097;Inherit;False;0;3;2;0.2053445,0.2046102,0.2075472,0;1.498039,1.05098,0,0.5029374;1.498039,1.05098,0,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;274;-2221.882,-661.3765;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;-1563.476,-1800.258;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;55;-1952.819,-1707.851;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;355;-741.9099,-57.94464;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;278;-1657.382,-616.6006;Inherit;False;NoiseIntensityGradient;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;300;963.2778,-1019.884;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;-213.9451,-1912.016;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;329;-1694.774,1182.17;Inherit;False;Constant;_Float0;Float 0;15;0;Create;True;0;0;0;False;0;False;3.78;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;325;-1537.905,1103.805;Inherit;False;Spherize;-1;;8;1488bb72d8899174ba0601b595d32b07;0;4;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GradientSampleNode;60;519.421,-1868.613;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;168;789.8558,843.5426;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleTimeNode;482;-4346.667,-905.5095;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;478;-4111.313,-863.4259;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;208;-2322.175,-2478.119;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;490;2255.329,-561.8025;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;171;-6538.293,-989.9619;Inherit;False;Property;_TimeInput;TimeInput;2;0;Create;True;0;0;0;False;0;False;0;181.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;93;-611.5009,-1869.268;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientNode;301;727.3349,-1020.595;Inherit;False;0;2;2;0.06572621,0.06572621,0.06603771,0;1,1,1,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.GetLocalVarNode;279;-1058.543,-536.5953;Inherit;False;278;NoiseIntensityGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;31;-2179.396,-2483.292;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;466;-3637.524,-646.1077;Inherit;False;365;ObjectScaledUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;332;-117.19,1054.924;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;492;1740.737,-651.9642;Inherit;False;484;BaseShapeMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;483;-4517.193,-901.4969;Inherit;False;Constant;_Float0;Float 0;16;0;Create;True;0;0;0;False;0;False;0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;165;602.972,841.0895;Inherit;False;222;flameMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;333;55.46613,951.8106;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;206;-1978.196,-1570.408;Inherit;False;Property;_NoiseGradientExponent;NoiseGradientExponent;3;0;Create;True;0;0;0;False;0;False;1;0.5;0.35;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;328;-359.1238,1049.172;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;358;726.5484,-939.5689;Inherit;False;356;CombinedNoise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;484;261.3955,-1970.34;Inherit;False;BaseShapeMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;331;-1199.862,1203.011;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;2,2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;360;-741.6279,-298.4509;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;364;1922.248,633.3984;Inherit;False;Constant;_Float1;Float 1;15;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;361;-510.8416,-295.7832;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;105;-355.5009,-1821.268;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;222;-829.1255,-1810.328;Inherit;False;flameMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;107;278.6259,-1861.741;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;489;1761.507,-790.8671;Inherit;False;0;5;2;0.3960785,0.07843138,0,0;0.8431373,0.2509804,0,0.197055;0.9803922,0.4156863,0,0.4088197;0.9960785,0.4862745,0.007843138,0.6470588;0.9921569,0.9921569,0.6078432,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.GetLocalVarNode;260;-333.0227,-543.0453;Inherit;True;259;BaseShape;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;327;-648.6984,1098.043;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;24;-3147.753,-2470.768;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,0.7;False;1;FLOAT2;0,0.25;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;303;2176.826,194.2231;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;439;1511.981,-430.8724;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0.7;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;322;-1527.684,845.7273;Inherit;False;Spherize;-1;;6;1488bb72d8899174ba0601b595d32b07;0;4;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;357;1720.748,196.5935;Inherit;False;356;CombinedNoise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;23;-2895.777,-2477.576;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;225;-1859.05,-2474.35;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;324;-638.4773,839.9652;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;275;-1999.26,-609.8624;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;326;-966.3513,1071.025;Inherit;True;Property;_TextureSample3;Texture Sample 3;14;0;Create;True;0;0;0;False;0;False;-1;None;fcbb22463ab66e3409012b2ef95a3ff0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;108;60.23726,-1756.679;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleRemainderNode;166;919.4695,845.2977;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;95;-73.13317,-2062.342;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;228;-2041.731,-2204.36;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;281;-1836.472,-608.6739;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-2697.363,-2478.578;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;51;-1129.733,-1979.621;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;259;-1531.768,-2449.526;Inherit;False;BaseShape;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;226;-2256.11,-2242.152;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;30;-2483.713,-2475.528;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;323;-956.1302,812.9475;Inherit;True;Property;_TextureSample2;Texture Sample 2;13;0;Create;True;0;0;0;False;0;False;-1;None;fcbb22463ab66e3409012b2ef95a3ff0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;137.2677,-106.4101;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;2446.803,39.72636;Float;False;True;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;ProceduralFlame;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;5;False;-1;10;False;-1;1;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;638198358813951047;  Blend;0;638198800201077325;Two Sided;1;0;Cast Shadows;0;638198359277842584;  Use Shadow Threshold;0;0;Receive Shadows;0;638198359297412382;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;DOTS Instancing;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,-1;0;  Type;0;0;  Tess;16,False,-1;0;  Min;10,False,-1;0;  Max;25,False,-1;0;  Edge Length;16,False,-1;0;  Max Displacement;25,False,-1;0;Vertex Position,InvertActionOnDeselection;1;0;0;5;False;True;False;True;False;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
WireConnection;116;0;115;1
WireConnection;116;1;115;2
WireConnection;128;0;116;0
WireConnection;392;0;132;2
WireConnection;131;0;392;0
WireConnection;131;1;129;0
WireConnection;388;0;132;1
WireConnection;388;1;131;0
WireConnection;125;0;179;2
WireConnection;125;1;124;0
WireConnection;126;0;125;0
WireConnection;145;0;388;0
WireConnection;175;0;145;1
WireConnection;175;1;126;0
WireConnection;146;0;145;0
WireConnection;146;1;175;0
WireConnection;270;0;146;0
WireConnection;316;1;318;0
WireConnection;310;0;271;0
WireConnection;310;1;316;0
WireConnection;319;0;314;2
WireConnection;347;0;314;1
WireConnection;390;0;389;0
WireConnection;344;0;310;0
WireConnection;346;0;344;0
WireConnection;346;1;347;0
WireConnection;379;0;390;1
WireConnection;379;1;380;0
WireConnection;313;0;344;1
WireConnection;313;1;319;0
WireConnection;497;0;496;0
WireConnection;348;0;346;0
WireConnection;348;1;313;0
WireConnection;374;0;379;0
WireConnection;499;0;497;1
WireConnection;499;1;498;0
WireConnection;312;0;310;0
WireConnection;312;1;348;0
WireConnection;312;2;315;0
WireConnection;381;0;400;0
WireConnection;381;1;374;0
WireConnection;474;0;381;0
WireConnection;500;0;499;0
WireConnection;365;0;312;0
WireConnection;502;0;501;0
WireConnection;502;1;500;0
WireConnection;470;0;467;0
WireConnection;475;0;465;0
WireConnection;503;0;502;0
WireConnection;472;0;470;0
WireConnection;472;1;465;0
WireConnection;476;0;470;0
WireConnection;476;1;475;0
WireConnection;477;0;476;0
WireConnection;477;1;504;0
WireConnection;473;0;472;0
WireConnection;473;1;470;1
WireConnection;248;0;477;0
WireConnection;248;1;247;0
WireConnection;240;0;473;0
WireConnection;240;1;241;0
WireConnection;250;0;253;0
WireConnection;250;1;248;0
WireConnection;239;0;253;0
WireConnection;239;1;240;0
WireConnection;258;0;239;1
WireConnection;257;0;250;1
WireConnection;263;0;258;0
WireConnection;262;0;257;0
WireConnection;371;0;390;0
WireConnection;371;1;381;0
WireConnection;267;0;264;0
WireConnection;267;1;285;0
WireConnection;369;0;371;0
WireConnection;295;0;291;0
WireConnection;295;1;290;0
WireConnection;403;0;369;0
WireConnection;280;0;267;0
WireConnection;280;1;282;0
WireConnection;446;0;444;2
WireConnection;293;0;295;0
WireConnection;293;1;289;0
WireConnection;395;0;403;0
WireConnection;395;1;405;2
WireConnection;362;0;280;0
WireConnection;362;1;293;0
WireConnection;449;0;446;0
WireConnection;449;1;456;0
WireConnection;384;0;395;0
WireConnection;268;0;362;0
WireConnection;445;0;449;0
WireConnection;356;0;268;0
WireConnection;461;0;460;2
WireConnection;457;0;437;0
WireConnection;452;0;451;0
WireConnection;441;0;432;0
WireConnection;441;1;452;0
WireConnection;441;2;495;0
WireConnection;459;0;457;0
WireConnection;459;1;461;0
WireConnection;433;0;459;0
WireConnection;433;1;441;0
WireConnection;438;0;433;0
WireConnection;493;0;438;0
WireConnection;302;0;493;0
WireConnection;302;1;62;0
WireConnection;205;0;55;2
WireConnection;205;1;206;0
WireConnection;56;1;205;0
WireConnection;355;0;280;0
WireConnection;355;1;293;0
WireConnection;278;0;281;0
WireConnection;300;0;301;0
WireConnection;300;1;358;0
WireConnection;103;0;105;0
WireConnection;325;5;329;0
WireConnection;60;1;107;0
WireConnection;168;0;165;0
WireConnection;482;0;483;0
WireConnection;478;0;470;1
WireConnection;478;1;482;0
WireConnection;208;0;30;0
WireConnection;490;0;489;0
WireConnection;490;1;493;0
WireConnection;31;0;208;0
WireConnection;332;0;328;0
WireConnection;328;0;324;0
WireConnection;328;1;327;0
WireConnection;484;0;95;0
WireConnection;331;0;325;0
WireConnection;360;0;280;0
WireConnection;360;1;293;0
WireConnection;361;0;360;0
WireConnection;105;0;93;2
WireConnection;222;0;51;0
WireConnection;107;0;95;0
WireConnection;107;1;108;0
WireConnection;327;0;326;1
WireConnection;303;0;302;0
WireConnection;439;0;438;0
WireConnection;23;0;24;0
WireConnection;225;0;31;0
WireConnection;225;1;228;0
WireConnection;324;0;323;1
WireConnection;275;0;274;2
WireConnection;326;1;331;0
WireConnection;108;0;105;0
WireConnection;166;0;165;0
WireConnection;95;0;51;0
WireConnection;95;1;103;0
WireConnection;228;0;226;2
WireConnection;281;0;275;0
WireConnection;29;0;23;0
WireConnection;51;0;225;0
WireConnection;51;1;56;0
WireConnection;259;0;225;0
WireConnection;30;0;29;0
WireConnection;323;1;322;0
WireConnection;1;2;490;0
WireConnection;1;3;303;0
ASEEND*/
//CHKSM=8D8C2AB4E05DA07CA69205EE95FC2B01B7D7C45E