// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "RaymarchingAmpTextureDepthNoise"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[ASEBegin]_NumSteps("NumSteps", Int) = 32
		_StepSize("StepSize", Float) = 0.02
		_densityScale("densityScale", Float) = 0.2
		_Volume("Volume", 3D) = "white" {}
		_Noise3D("Noise3D", 3D) = "white" {}
		_offset("offset", Vector) = (0.5,0.5,0.5,0)
		_numLightSteps("numLightSteps", Int) = 16
		_lightStepSize("lightStepSize", Float) = 0.06
		_LightAsorb("LightAsorb", Float) = 2
		_DarknessThreshold("DarknessThreshold", Float) = 0
		_transmittance("transmittance", Float) = 1
		_ShadeCol("ShadeCol", Color) = (0,0,0,0)
		_LightCol("LightCol", Color) = (1,0,0,0)
		_AlphaScalar("AlphaScalar", Range( 0 , 1)) = 1
		_NoiseOffset("Noise Offset", Vector) = (0,1,0,0)
		_NoiseStr("Noise Str", Range( 0 , 6)) = 0
		_NoiseScale("NoiseScale", Range( 0 , 5)) = 1
		[ASEEnd]_NoiseDenistyScale("NoiseDenistyScale", Range( 0 , 5)) = 1

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
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			
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

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
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
			float4 _ShadeCol;
			float4 _LightCol;
			float3 _offset;
			float3 _NoiseOffset;
			int _NumSteps;
			float _StepSize;
			float _densityScale;
			int _numLightSteps;
			float _lightStepSize;
			float _LightAsorb;
			float _DarknessThreshold;
			float _transmittance;
			float _NoiseStr;
			float _NoiseScale;
			float _NoiseDenistyScale;
			float _AlphaScalar;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler3D _Volume;
			uniform float4 _CameraDepthTexture_TexelSize;
			sampler3D _Noise3D;


			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex ];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
			}
			
			float3 InvertDepthDirURP75_g119( float3 In )
			{
				float3 result = In;
				#if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301 || ASE_SRP_VERSION == 70503 || ASE_SRP_VERSION == 70600 || ASE_SRP_VERSION == 70700 || ASE_SRP_VERSION == 70701 || ASE_SRP_VERSION >= 80301
				result *= float3(1,1,-1);
				#endif
				return result;
			}
			
			float3 Raymarch9( float3 rayOrigin, float3 rayDirection, int numSteps, float stepSize, float densityScale, sampler3D Volume, float3 offset, int numLightSteps, float lightStepSize, float3 lightDir, float lightAsorb, float darknessThreshold, float transmittance, float3 depthPos, sampler3D Noise3D, float3 noiseOffset, float noiseStr, float noiseScale, float noiseDensityScalar )
			{
				 float denisty = 0;
				    float transmission = 0;
				    float lightAccumulation = 0;
				    float finalLight = 0;
				        
				    // Distance
				    float totalDist = 0;
				    float distToDepth = length(rayOrigin - depthPos);
				    
				     
				    for (int i = 0; i < numSteps; i++)
				    {
				        if (totalDist > distToDepth)
				            break;
				            
				        totalDist += stepSize;
				        rayOrigin += rayDirection * stepSize;
				    
				        float3 samplingPos = rayOrigin+offset;
				        float4 samplePosMip = float4(samplingPos, 0);
				        
				        // Sample noise to offset ray/uv
				        float4 noiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
				        float noiseSample = pow(tex3Dlod(Noise3D,noiseSamplePos).r,noiseStr);
				        
				        // Sample smoke volume tex
				        float noiseLookupOffset = noiseSample * normalize(noiseOffset) * .1 * noiseStr;
				        samplePosMip.xyz += noiseLookupOffset;
				        float sampleDensity = tex3Dlod(Volume,samplePosMip).r;
				        
				        // Scale the noise density contribution
				        sampleDensity *= noiseSample * noiseDensityScalar;
				        denisty += sampleDensity * densityScale;
				        
				        
				    
				        // Lighting
				        float3 lightRayOrigin = samplingPos;
				        for (int j = 0; j < numLightSteps; j++)
				        {
				            lightRayOrigin += lightDir * lightStepSize;
				            float4 lightRayOriginMip = float4(lightRayOrigin,0);
				            
				            // Sample noise
				           // float4 lightNoiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
				           // float lightNoiseSample = pow(tex3Dlod(Noise3D,lightNoiseSamplePos).r,noiseStr);
				            
				            // Sample volume tex
				           // noiseLookupOffset = lightNoiseSample * normalize(noiseOffset) * .1 * noiseStr;
				           // lightRayOriginMip.xyz += noiseLookupOffset;
				            float lightDensity = tex3Dlod(Volume, lightRayOriginMip).r;
				            
				            // Scale the noise density contribution
				           // lightDensity  *= lightNoiseSample * noiseDensityScalar;
				            lightAccumulation += lightDensity * densityScale; 
				        }
				    
				        float lightTransmission = exp(-lightAccumulation);
				        float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
				        finalLight += denisty * transmittance * shadow;
				        transmittance *= exp(-denisty*lightAsorb);
				    }
				    
				    transmission = exp(-denisty);
				    return float3(finalLight, transmission, transmittance);
			}
			
			
			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord3 = screenPos;
				
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
				float3 rayOrigin9 = WorldPosition;
				float3 normalizeResult42 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float3 rayDirection9 = normalizeResult42;
				int numSteps9 = _NumSteps;
				float stepSize9 = _StepSize;
				float densityScale9 = _densityScale;
				sampler3D Volume9 = _Volume;
				float4 transform19 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float3 offset9 = ( float4( _offset , 0.0 ) - transform19 ).xyz;
				int numLightSteps9 = _numLightSteps;
				float lightStepSize9 = _lightStepSize;
				float3 lightDir9 = _MainLightPosition.xyz;
				float lightAsorb9 = _LightAsorb;
				float darknessThreshold9 = _DarknessThreshold;
				float transmittance9 = _transmittance;
				float4 screenPos = IN.ase_texcoord3;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV22_g120 = ase_screenPosNorm.xy;
				float2 localUnStereo22_g120 = UnStereo( UV22_g120 );
				float2 break64_g119 = localUnStereo22_g120;
				float clampDepth69_g119 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch38_g119 = ( 1.0 - clampDepth69_g119 );
				#else
				float staticSwitch38_g119 = clampDepth69_g119;
				#endif
				float3 appendResult39_g119 = (float3(break64_g119.x , break64_g119.y , staticSwitch38_g119));
				float4 appendResult42_g119 = (float4((appendResult39_g119*2.0 + -1.0) , 1.0));
				float4 temp_output_43_0_g119 = mul( unity_CameraInvProjection, appendResult42_g119 );
				float3 temp_output_46_0_g119 = ( (temp_output_43_0_g119).xyz / (temp_output_43_0_g119).w );
				float3 In75_g119 = temp_output_46_0_g119;
				float3 localInvertDepthDirURP75_g119 = InvertDepthDirURP75_g119( In75_g119 );
				float4 appendResult49_g119 = (float4(localInvertDepthDirURP75_g119 , 1.0));
				float3 depthPos9 = (mul( unity_CameraToWorld, appendResult49_g119 )).xyz;
				sampler3D Noise3D9 = _Noise3D;
				float3 noiseOffset9 = ( _NoiseOffset * _TimeParameters.x );
				float noiseStr9 = _NoiseStr;
				float noiseScale9 = _NoiseScale;
				float noiseDensityScalar9 = _NoiseDenistyScale;
				float3 localRaymarch9 = Raymarch9( rayOrigin9 , rayDirection9 , numSteps9 , stepSize9 , densityScale9 , Volume9 , offset9 , numLightSteps9 , lightStepSize9 , lightDir9 , lightAsorb9 , darknessThreshold9 , transmittance9 , depthPos9 , Noise3D9 , noiseOffset9 , noiseStr9 , noiseScale9 , noiseDensityScalar9 );
				float3 break27 = localRaymarch9;
				float4 lerpResult32 = lerp( _ShadeCol , _LightCol , break27.x);
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = lerpResult32.rgb;
				float Alpha = ( ( 1.0 - break27.y ) * _AlphaScalar );
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
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			ColorMask 0

			HLSLPROGRAM
			
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			
			#pragma vertex vert
			#pragma fragment frag
#if ASE_SRP_VERSION >= 110000
			#pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#endif
			#define SHADERPASS SHADERPASS_SHADOWCASTER

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
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
			float4 _ShadeCol;
			float4 _LightCol;
			float3 _offset;
			float3 _NoiseOffset;
			int _NumSteps;
			float _StepSize;
			float _densityScale;
			int _numLightSteps;
			float _lightStepSize;
			float _LightAsorb;
			float _DarknessThreshold;
			float _transmittance;
			float _NoiseStr;
			float _NoiseScale;
			float _NoiseDenistyScale;
			float _AlphaScalar;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler3D _Volume;
			uniform float4 _CameraDepthTexture_TexelSize;
			sampler3D _Noise3D;


			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex ];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
			}
			
			float3 InvertDepthDirURP75_g119( float3 In )
			{
				float3 result = In;
				#if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301 || ASE_SRP_VERSION == 70503 || ASE_SRP_VERSION == 70600 || ASE_SRP_VERSION == 70700 || ASE_SRP_VERSION == 70701 || ASE_SRP_VERSION >= 80301
				result *= float3(1,1,-1);
				#endif
				return result;
			}
			
			float3 Raymarch9( float3 rayOrigin, float3 rayDirection, int numSteps, float stepSize, float densityScale, sampler3D Volume, float3 offset, int numLightSteps, float lightStepSize, float3 lightDir, float lightAsorb, float darknessThreshold, float transmittance, float3 depthPos, sampler3D Noise3D, float3 noiseOffset, float noiseStr, float noiseScale, float noiseDensityScalar )
			{
				 float denisty = 0;
				    float transmission = 0;
				    float lightAccumulation = 0;
				    float finalLight = 0;
				        
				    // Distance
				    float totalDist = 0;
				    float distToDepth = length(rayOrigin - depthPos);
				    
				     
				    for (int i = 0; i < numSteps; i++)
				    {
				        if (totalDist > distToDepth)
				            break;
				            
				        totalDist += stepSize;
				        rayOrigin += rayDirection * stepSize;
				    
				        float3 samplingPos = rayOrigin+offset;
				        float4 samplePosMip = float4(samplingPos, 0);
				        
				        // Sample noise to offset ray/uv
				        float4 noiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
				        float noiseSample = pow(tex3Dlod(Noise3D,noiseSamplePos).r,noiseStr);
				        
				        // Sample smoke volume tex
				        float noiseLookupOffset = noiseSample * normalize(noiseOffset) * .1 * noiseStr;
				        samplePosMip.xyz += noiseLookupOffset;
				        float sampleDensity = tex3Dlod(Volume,samplePosMip).r;
				        
				        // Scale the noise density contribution
				        sampleDensity *= noiseSample * noiseDensityScalar;
				        denisty += sampleDensity * densityScale;
				        
				        
				    
				        // Lighting
				        float3 lightRayOrigin = samplingPos;
				        for (int j = 0; j < numLightSteps; j++)
				        {
				            lightRayOrigin += lightDir * lightStepSize;
				            float4 lightRayOriginMip = float4(lightRayOrigin,0);
				            
				            // Sample noise
				           // float4 lightNoiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
				           // float lightNoiseSample = pow(tex3Dlod(Noise3D,lightNoiseSamplePos).r,noiseStr);
				            
				            // Sample volume tex
				           // noiseLookupOffset = lightNoiseSample * normalize(noiseOffset) * .1 * noiseStr;
				           // lightRayOriginMip.xyz += noiseLookupOffset;
				            float lightDensity = tex3Dlod(Volume, lightRayOriginMip).r;
				            
				            // Scale the noise density contribution
				           // lightDensity  *= lightNoiseSample * noiseDensityScalar;
				            lightAccumulation += lightDensity * densityScale; 
				        }
				    
				        float lightTransmission = exp(-lightAccumulation);
				        float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
				        finalLight += denisty * transmittance * shadow;
				        transmittance *= exp(-denisty*lightAsorb);
				    }
				    
				    transmission = exp(-denisty);
				    return float3(finalLight, transmission, transmittance);
			}
			

			float3 _LightDirection;
#if ASE_SRP_VERSION >= 110000 
			float3 _LightPosition;
#endif
			VertexOutput VertexFunction( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord2 = screenPos;
				
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

				float3 normalWS = TransformObjectToWorldDir( v.ase_normal );
#if ASE_SRP_VERSION >= 110000 
			#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 lightDirectionWS = normalize(_LightPosition - positionWS);
			#else
				float3 lightDirectionWS = _LightDirection;
			#endif
				float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
			#if UNITY_REVERSED_Z
				clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
			#else
				clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
			#endif
#else
				float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );
				#if UNITY_REVERSED_Z
					clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#else
					clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#endif
#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = clipPos;

				return o;
			}
			
			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				
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

				float3 rayOrigin9 = WorldPosition;
				float3 normalizeResult42 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float3 rayDirection9 = normalizeResult42;
				int numSteps9 = _NumSteps;
				float stepSize9 = _StepSize;
				float densityScale9 = _densityScale;
				sampler3D Volume9 = _Volume;
				float4 transform19 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float3 offset9 = ( float4( _offset , 0.0 ) - transform19 ).xyz;
				int numLightSteps9 = _numLightSteps;
				float lightStepSize9 = _lightStepSize;
				float3 lightDir9 = _MainLightPosition.xyz;
				float lightAsorb9 = _LightAsorb;
				float darknessThreshold9 = _DarknessThreshold;
				float transmittance9 = _transmittance;
				float4 screenPos = IN.ase_texcoord2;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV22_g120 = ase_screenPosNorm.xy;
				float2 localUnStereo22_g120 = UnStereo( UV22_g120 );
				float2 break64_g119 = localUnStereo22_g120;
				float clampDepth69_g119 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch38_g119 = ( 1.0 - clampDepth69_g119 );
				#else
				float staticSwitch38_g119 = clampDepth69_g119;
				#endif
				float3 appendResult39_g119 = (float3(break64_g119.x , break64_g119.y , staticSwitch38_g119));
				float4 appendResult42_g119 = (float4((appendResult39_g119*2.0 + -1.0) , 1.0));
				float4 temp_output_43_0_g119 = mul( unity_CameraInvProjection, appendResult42_g119 );
				float3 temp_output_46_0_g119 = ( (temp_output_43_0_g119).xyz / (temp_output_43_0_g119).w );
				float3 In75_g119 = temp_output_46_0_g119;
				float3 localInvertDepthDirURP75_g119 = InvertDepthDirURP75_g119( In75_g119 );
				float4 appendResult49_g119 = (float4(localInvertDepthDirURP75_g119 , 1.0));
				float3 depthPos9 = (mul( unity_CameraToWorld, appendResult49_g119 )).xyz;
				sampler3D Noise3D9 = _Noise3D;
				float3 noiseOffset9 = ( _NoiseOffset * _TimeParameters.x );
				float noiseStr9 = _NoiseStr;
				float noiseScale9 = _NoiseScale;
				float noiseDensityScalar9 = _NoiseDenistyScale;
				float3 localRaymarch9 = Raymarch9( rayOrigin9 , rayDirection9 , numSteps9 , stepSize9 , densityScale9 , Volume9 , offset9 , numLightSteps9 , lightStepSize9 , lightDir9 , lightAsorb9 , darknessThreshold9 , transmittance9 , depthPos9 , Noise3D9 , noiseOffset9 , noiseStr9 , noiseScale9 , noiseDensityScalar9 );
				float3 break27 = localRaymarch9;
				
				float Alpha = ( ( 1.0 - break27.y ) * _AlphaScalar );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					#ifdef _ALPHATEST_SHADOW_ON
						clip(Alpha - AlphaClipThresholdShadow);
					#else
						clip(Alpha - AlphaClipThreshold);
					#endif
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
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
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
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
			float4 _ShadeCol;
			float4 _LightCol;
			float3 _offset;
			float3 _NoiseOffset;
			int _NumSteps;
			float _StepSize;
			float _densityScale;
			int _numLightSteps;
			float _lightStepSize;
			float _LightAsorb;
			float _DarknessThreshold;
			float _transmittance;
			float _NoiseStr;
			float _NoiseScale;
			float _NoiseDenistyScale;
			float _AlphaScalar;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler3D _Volume;
			uniform float4 _CameraDepthTexture_TexelSize;
			sampler3D _Noise3D;


			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex ];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
			}
			
			float3 InvertDepthDirURP75_g119( float3 In )
			{
				float3 result = In;
				#if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301 || ASE_SRP_VERSION == 70503 || ASE_SRP_VERSION == 70600 || ASE_SRP_VERSION == 70700 || ASE_SRP_VERSION == 70701 || ASE_SRP_VERSION >= 80301
				result *= float3(1,1,-1);
				#endif
				return result;
			}
			
			float3 Raymarch9( float3 rayOrigin, float3 rayDirection, int numSteps, float stepSize, float densityScale, sampler3D Volume, float3 offset, int numLightSteps, float lightStepSize, float3 lightDir, float lightAsorb, float darknessThreshold, float transmittance, float3 depthPos, sampler3D Noise3D, float3 noiseOffset, float noiseStr, float noiseScale, float noiseDensityScalar )
			{
				 float denisty = 0;
				    float transmission = 0;
				    float lightAccumulation = 0;
				    float finalLight = 0;
				        
				    // Distance
				    float totalDist = 0;
				    float distToDepth = length(rayOrigin - depthPos);
				    
				     
				    for (int i = 0; i < numSteps; i++)
				    {
				        if (totalDist > distToDepth)
				            break;
				            
				        totalDist += stepSize;
				        rayOrigin += rayDirection * stepSize;
				    
				        float3 samplingPos = rayOrigin+offset;
				        float4 samplePosMip = float4(samplingPos, 0);
				        
				        // Sample noise to offset ray/uv
				        float4 noiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
				        float noiseSample = pow(tex3Dlod(Noise3D,noiseSamplePos).r,noiseStr);
				        
				        // Sample smoke volume tex
				        float noiseLookupOffset = noiseSample * normalize(noiseOffset) * .1 * noiseStr;
				        samplePosMip.xyz += noiseLookupOffset;
				        float sampleDensity = tex3Dlod(Volume,samplePosMip).r;
				        
				        // Scale the noise density contribution
				        sampleDensity *= noiseSample * noiseDensityScalar;
				        denisty += sampleDensity * densityScale;
				        
				        
				    
				        // Lighting
				        float3 lightRayOrigin = samplingPos;
				        for (int j = 0; j < numLightSteps; j++)
				        {
				            lightRayOrigin += lightDir * lightStepSize;
				            float4 lightRayOriginMip = float4(lightRayOrigin,0);
				            
				            // Sample noise
				           // float4 lightNoiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0);
				           // float lightNoiseSample = pow(tex3Dlod(Noise3D,lightNoiseSamplePos).r,noiseStr);
				            
				            // Sample volume tex
				           // noiseLookupOffset = lightNoiseSample * normalize(noiseOffset) * .1 * noiseStr;
				           // lightRayOriginMip.xyz += noiseLookupOffset;
				            float lightDensity = tex3Dlod(Volume, lightRayOriginMip).r;
				            
				            // Scale the noise density contribution
				           // lightDensity  *= lightNoiseSample * noiseDensityScalar;
				            lightAccumulation += lightDensity * densityScale; 
				        }
				    
				        float lightTransmission = exp(-lightAccumulation);
				        float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold);
				        finalLight += denisty * transmittance * shadow;
				        transmittance *= exp(-denisty*lightAsorb);
				    }
				    
				    transmission = exp(-denisty);
				    return float3(finalLight, transmission, transmittance);
			}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord2 = screenPos;
				
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

				float3 rayOrigin9 = WorldPosition;
				float3 normalizeResult42 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float3 rayDirection9 = normalizeResult42;
				int numSteps9 = _NumSteps;
				float stepSize9 = _StepSize;
				float densityScale9 = _densityScale;
				sampler3D Volume9 = _Volume;
				float4 transform19 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float3 offset9 = ( float4( _offset , 0.0 ) - transform19 ).xyz;
				int numLightSteps9 = _numLightSteps;
				float lightStepSize9 = _lightStepSize;
				float3 lightDir9 = _MainLightPosition.xyz;
				float lightAsorb9 = _LightAsorb;
				float darknessThreshold9 = _DarknessThreshold;
				float transmittance9 = _transmittance;
				float4 screenPos = IN.ase_texcoord2;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV22_g120 = ase_screenPosNorm.xy;
				float2 localUnStereo22_g120 = UnStereo( UV22_g120 );
				float2 break64_g119 = localUnStereo22_g120;
				float clampDepth69_g119 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch38_g119 = ( 1.0 - clampDepth69_g119 );
				#else
				float staticSwitch38_g119 = clampDepth69_g119;
				#endif
				float3 appendResult39_g119 = (float3(break64_g119.x , break64_g119.y , staticSwitch38_g119));
				float4 appendResult42_g119 = (float4((appendResult39_g119*2.0 + -1.0) , 1.0));
				float4 temp_output_43_0_g119 = mul( unity_CameraInvProjection, appendResult42_g119 );
				float3 temp_output_46_0_g119 = ( (temp_output_43_0_g119).xyz / (temp_output_43_0_g119).w );
				float3 In75_g119 = temp_output_46_0_g119;
				float3 localInvertDepthDirURP75_g119 = InvertDepthDirURP75_g119( In75_g119 );
				float4 appendResult49_g119 = (float4(localInvertDepthDirURP75_g119 , 1.0));
				float3 depthPos9 = (mul( unity_CameraToWorld, appendResult49_g119 )).xyz;
				sampler3D Noise3D9 = _Noise3D;
				float3 noiseOffset9 = ( _NoiseOffset * _TimeParameters.x );
				float noiseStr9 = _NoiseStr;
				float noiseScale9 = _NoiseScale;
				float noiseDensityScalar9 = _NoiseDenistyScale;
				float3 localRaymarch9 = Raymarch9( rayOrigin9 , rayDirection9 , numSteps9 , stepSize9 , densityScale9 , Volume9 , offset9 , numLightSteps9 , lightStepSize9 , lightDir9 , lightAsorb9 , darknessThreshold9 , transmittance9 , depthPos9 , Noise3D9 , noiseOffset9 , noiseStr9 , noiseScale9 , noiseDensityScalar9 );
				float3 break27 = localRaymarch9;
				
				float Alpha = ( ( 1.0 - break27.y ) * _AlphaScalar );
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
585;341;1535;830;1268.191;-85.53392;1.002847;True;False
Node;AmplifyShaderEditor.CommentaryNode;25;-1674.336,15.94854;Inherit;False;477.5408;601.1575;Tex and offset;4;19;17;18;16;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;37;-2026.959,1302.863;Inherit;False;817.41;420.174;max dist from depth ;2;38;41;max dist from depth ;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;5;-1410.796,-655.5516;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos;6;-1474.796,-506.5516;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleTimeNode;46;-779.0237,1326.645;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;17;-1603.536,246.3057;Inherit;False;Property;_offset;offset;5;0;Create;True;0;0;0;False;0;False;0.5,0.5,0.5;0.5,0.5,0.5;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;38;-1985.103,1505.592;Inherit;True;Reconstruct World Position From Depth;-1;;119;e7094bcbcc80eb140b2a3dbe6a861de8;0;0;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;10;-1192.551,-476.9699;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;26;-1440.296,-350.5515;Inherit;False;243;346.6;Steps and density;3;7;8;12;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ObjectToWorldTransfNode;19;-1624.336,410.1055;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector3Node;45;-767.2767,1144.557;Inherit;False;Property;_NoiseOffset;Noise Offset;14;0;Create;True;0;0;0;False;0;False;0,1,0;-0.1,-0.3,0.1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;24;-1492.131,632.7584;Inherit;False;283.6;654.7991;Lighting;6;30;28;21;22;23;29;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-1362.334,1207.16;Inherit;False;Property;_transmittance;transmittance;10;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-1425.032,1117.86;Inherit;False;Property;_DarknessThreshold;DarknessThreshold;9;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;16;-1448.796,65.94821;Inherit;True;Property;_Volume;Volume;3;0;Create;True;0;0;0;False;0;False;ccce97dfb33be7a4bbe15dc0a0275c94;ccce97dfb33be7a4bbe15dc0a0275c94;False;white;LockedToTexture3D;Texture3D;-1;0;2;SAMPLER3D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleSubtractOpNode;18;-1364.337,278.8057;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.IntNode;21;-1426.93,682.7585;Inherit;False;Property;_numLightSteps;numLightSteps;6;0;Create;True;0;0;0;False;0;False;16;16;False;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-526.5992,1234.701;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;43;-807.1812,948.0935;Inherit;True;Property;_Noise3D;Noise3D;4;0;Create;True;0;0;0;False;0;False;4ed99a7ccafd728408a1bd580f472384;4ed99a7ccafd728408a1bd580f472384;False;white;LockedToTexture3D;Texture3D;-1;0;2;SAMPLER3D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;50;-319.4974,837.669;Inherit;False;Property;_NoiseDenistyScale;NoiseDenistyScale;17;0;Create;True;0;0;0;False;0;False;1;0.67;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-1413.63,768.2577;Inherit;False;Property;_lightStepSize;lightStepSize;7;0;Create;True;0;0;0;False;0;False;0.06;0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;23;-1442.131,884.1581;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.IntNode;7;-1385.796,-300.5515;Inherit;False;Property;_NumSteps;NumSteps;0;0;Create;True;0;0;0;False;0;False;32;32;False;0;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-330.5288,754.4328;Inherit;False;Property;_NoiseScale;NoiseScale;16;0;Create;True;0;0;0;False;0;False;1;1.148;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-595.6081,1508.521;Inherit;False;Property;_NoiseStr;Noise Str;15;0;Create;True;0;0;0;False;0;False;0;2.489;0;6;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;41;-1633.103,1521.592;Inherit;False;True;True;True;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;42;-1046.875,-467.5729;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-1390.296,-119.9515;Inherit;False;Property;_densityScale;densityScale;2;0;Create;True;0;0;0;False;0;False;0.2;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-1371.832,1036.159;Inherit;False;Property;_LightAsorb;LightAsorb;8;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-1379.796,-207.5515;Inherit;False;Property;_StepSize;StepSize;1;0;Create;True;0;0;0;False;0;False;0.02;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;9;-433.661,-55.72034;Inherit;False; float denisty = 0@$    float transmission = 0@$    float lightAccumulation = 0@$    float finalLight = 0@$        $    // Distance$    float totalDist = 0@$    float distToDepth = length(rayOrigin - depthPos)@$    $     $    for (int i = 0@ i < numSteps@ i++)$    {$        if (totalDist > distToDepth)$            break@$            $        totalDist += stepSize@$        rayOrigin += rayDirection * stepSize@$    $        float3 samplingPos = rayOrigin+offset@$        float4 samplePosMip = float4(samplingPos, 0)@$        $        // Sample noise to offset ray/uv$        float4 noiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0)@$        float noiseSample = pow(tex3Dlod(Noise3D,noiseSamplePos).r,noiseStr)@$        $        // Sample smoke volume tex$        float noiseLookupOffset = noiseSample * normalize(noiseOffset) * .1 * noiseStr@$        samplePosMip.xyz += noiseLookupOffset@$        float sampleDensity = tex3Dlod(Volume,samplePosMip).r@$        $        // Scale the noise density contribution$        sampleDensity *= noiseSample * noiseDensityScalar@$        denisty += sampleDensity * densityScale@$        $        $    $        // Lighting$        float3 lightRayOrigin = samplingPos@$        for (int j = 0@ j < numLightSteps@ j++)$        {$            lightRayOrigin += lightDir * lightStepSize@$            float4 lightRayOriginMip = float4(lightRayOrigin,0)@$            $            // Sample noise$           // float4 lightNoiseSamplePos =  float4(samplingPos * noiseScale + noiseOffset, 0)@$           // float lightNoiseSample = pow(tex3Dlod(Noise3D,lightNoiseSamplePos).r,noiseStr)@$            $            // Sample volume tex$           // noiseLookupOffset = lightNoiseSample * normalize(noiseOffset) * .1 * noiseStr@$           // lightRayOriginMip.xyz += noiseLookupOffset@$            float lightDensity = tex3Dlod(Volume, lightRayOriginMip).r@$            $            // Scale the noise density contribution$           // lightDensity  *= lightNoiseSample * noiseDensityScalar@$            lightAccumulation += lightDensity * densityScale@ $        }$    $        float lightTransmission = exp(-lightAccumulation)@$        float shadow = darknessThreshold + lightTransmission * (1.0 - darknessThreshold)@$        finalLight += denisty * transmittance * shadow@$        transmittance *= exp(-denisty*lightAsorb)@$    }$    $    transmission = exp(-denisty)@$    return float3(finalLight, transmission, transmittance)@;3;Create;19;True;rayOrigin;FLOAT3;0,0,0;In;;Inherit;False;True;rayDirection;FLOAT3;0,0,0;In;;Inherit;False;True;numSteps;INT;0;In;;Inherit;False;True;stepSize;FLOAT;0;In;;Inherit;False;True;densityScale;FLOAT;0;In;;Inherit;False;True;Volume;SAMPLER3D;;In;;Inherit;False;True;offset;FLOAT3;0,0,0;In;;Inherit;False;True;numLightSteps;INT;0;In;;Inherit;False;True;lightStepSize;FLOAT;0;In;;Inherit;False;True;lightDir;FLOAT3;0,0,0;In;;Inherit;False;True;lightAsorb;FLOAT;0;In;;Inherit;False;True;darknessThreshold;FLOAT;0;In;;Inherit;False;True;transmittance;FLOAT;0;In;;Inherit;False;True;depthPos;FLOAT3;0,0,0;In;;Inherit;False;True;Noise3D;SAMPLER3D;;In;;Inherit;False;True;noiseOffset;FLOAT3;0,0,0;In;;Inherit;False;True;noiseStr;FLOAT;1;In;;Inherit;False;True;noiseScale;FLOAT;1;In;;Inherit;False;True;noiseDensityScalar;FLOAT;1;In;;Inherit;False;Raymarch;True;False;0;;False;19;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;INT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;SAMPLER3D;;False;6;FLOAT3;0,0,0;False;7;INT;0;False;8;FLOAT;0;False;9;FLOAT3;0,0,0;False;10;FLOAT;0;False;11;FLOAT;0;False;12;FLOAT;0;False;13;FLOAT3;0,0,0;False;14;SAMPLER3D;;False;15;FLOAT3;0,0,0;False;16;FLOAT;1;False;17;FLOAT;1;False;18;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode;27;-71.83575,-81.48961;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;35;47.41541,95.02319;Inherit;False;Property;_AlphaScalar;AlphaScalar;13;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;31;49.41541,-20.97681;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;34;-84.58459,-295.9768;Inherit;False;Property;_LightCol;LightCol;12;0;Create;True;0;0;0;False;0;False;1,0,0,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;368.4154,29.02319;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;32;149.4154,-300.9768;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;33;-83.58459,-474.9768;Inherit;False;Property;_ShadeCol;ShadeCol;11;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.03773582,0.03773582,0.03773582,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;568.3,-93.1;Float;False;True;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;3;RaymarchingAmpTextureDepthNoise;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;5;False;-1;10;False;-1;1;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;638232038898469436;  Blend;0;0;Two Sided;1;0;Cast Shadows;1;0;  Use Shadow Threshold;0;0;Receive Shadows;1;0;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;DOTS Instancing;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,-1;0;  Type;0;0;  Tess;16,False,-1;0;  Min;10,False,-1;0;  Max;25,False,-1;0;  Edge Length;16,False,-1;0;  Max Displacement;25,False,-1;0;Vertex Position,InvertActionOnDeselection;1;0;0;5;False;True;True;True;False;False;;False;0
WireConnection;10;0;5;0
WireConnection;10;1;6;0
WireConnection;18;0;17;0
WireConnection;18;1;19;0
WireConnection;47;0;45;0
WireConnection;47;1;46;0
WireConnection;41;0;38;0
WireConnection;42;0;10;0
WireConnection;9;0;5;0
WireConnection;9;1;42;0
WireConnection;9;2;7;0
WireConnection;9;3;8;0
WireConnection;9;4;12;0
WireConnection;9;5;16;0
WireConnection;9;6;18;0
WireConnection;9;7;21;0
WireConnection;9;8;22;0
WireConnection;9;9;23;0
WireConnection;9;10;28;0
WireConnection;9;11;29;0
WireConnection;9;12;30;0
WireConnection;9;13;41;0
WireConnection;9;14;43;0
WireConnection;9;15;47;0
WireConnection;9;16;48;0
WireConnection;9;17;49;0
WireConnection;9;18;50;0
WireConnection;27;0;9;0
WireConnection;31;0;27;1
WireConnection;36;0;31;0
WireConnection;36;1;35;0
WireConnection;32;0;33;0
WireConnection;32;1;34;0
WireConnection;32;2;27;0
WireConnection;1;2;32;0
WireConnection;1;3;36;0
ASEEND*/
//CHKSM=CE6C7BB8A8BE6DE9A79F46BC1AEC32310F824474