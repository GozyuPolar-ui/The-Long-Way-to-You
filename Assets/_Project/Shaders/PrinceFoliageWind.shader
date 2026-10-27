Shader "Prince/Foliage Wind (URP Lit)"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 1
        _Metallic("Metallic", Range(0,1)) = 0
        _MetallicGlossMap("Metallic Map", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0
        _GlossMapScale("Smoothness Scale", Range(0,1)) = 1
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "white" {}
        _SpecColor("Specular Color", Color) = (0.2,0.2,0.2,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [HideInInspector] _ReceiveShadows("Receive Shadows", Float) = 1
        [Toggle(_RECEIVE_SHADOWS_OFF)] _DisableReceiveShadows("Disable Receive Shadows", Float) = 0
        [Toggle(_SPECULARHIGHLIGHTS_OFF)] _DisableSpecularHighlights("Disable Specular Highlights", Float) = 0
        [Toggle(_ENVIRONMENTREFLECTIONS_OFF)] _DisableEnvironmentReflections("Disable Environment Reflections", Float) = 0

        [Header(Wind)]
        [Enum(Grass,0,Bush,1,TreeLeaves,2,TreeTrunk,3)] _WindMode("Wind Mode", Float) = 0
        _WindStrength("Local Wind Strength", Range(0,2)) = 0.35
        _WindSpeed("Local Wind Speed", Range(0,4)) = 1
        _WindFrequency("Local Wind Frequency", Range(0,8)) = 1
        _WindDirection("Local Wind Direction (XZ)", Vector) = (0,0,0,0)
        _FlutterStrength("Flutter Strength", Range(0,1)) = 0.12
        _HeightMask("Height Mask Start", Range(0,0.95)) = 0.12
        _TrunkStiffness("Trunk Stiffness", Range(0,1)) = 0.9
        _WindHeightMin("Wind Height Minimum", Float) = 0
        _WindHeightRange("Wind Height Range", Float) = 1
        _WindMaskPower("Wind Mask Power", Range(0.5,5)) = 1.5

        [HideInInspector] _Surface("__surface", Float) = 0
        [HideInInspector] _Blend("__blend", Float) = 0
        [HideInInspector] _SrcBlend("__src", Float) = 1
        [HideInInspector] _DstBlend("__dst", Float) = 0
        [HideInInspector] _ZWrite("__zw", Float) = 1
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0
        [HideInInspector] _QueueOffset("__queueOffset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex FoliageVertex
            #pragma fragment FoliageFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _SpecColor;
                float4 _WindDirection;
                float4 _BaseMap_TexelSize;
                float _Cutoff;
                float _AlphaClip;
                float _Metallic;
                float _Smoothness;
                float _GlossMapScale;
                float _BumpScale;
                float _OcclusionStrength;
                float _Cull;
                float _ReceiveShadows;
                float _DisableReceiveShadows;
                float _DisableSpecularHighlights;
                float _DisableEnvironmentReflections;
                float _Surface;
                float _Blend;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _AlphaToMask;
                float _QueueOffset;
                float _WindMode;
                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float _FlutterStrength;
                float _HeightMask;
                float _TrunkStiffness;
                float _WindHeightMin;
                float _WindHeightRange;
                float _WindMaskPower;
            CBUFFER_END

            float4 _GlobalWindDirection;
            float _GlobalWindStrength;
            float _GlobalWindSpeed;
            float _GlobalGustStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float3 ApplyFoliageWind(float3 positionOS, float3 positionWS, out float3 displacedWS)
            {
                float height = saturate((positionOS.y - _WindHeightMin) / max(_WindHeightRange, 0.001));
                float heightWeight = saturate((height - _HeightMask) / max(1.0 - _HeightMask, 0.001));
                float modePower = _WindMode < 0.5 ? 0.75 : (_WindMode < 1.5 ? 1.4 : 2.2);
                float bend = pow(heightWeight, max(_WindMaskPower, modePower));
                float trunkScale = _WindMode > 2.5 ? (1.0 - _TrunkStiffness) * 0.12 : 1.0;
                float3 instancePositionWS = TransformObjectToWorld(float3(0, 0, 0));
                float phase = Hash21(instancePositionWS.xz);
                float timeValue = _Time.y * _GlobalWindSpeed * _WindSpeed;
                float slowWave = sin(timeValue * max(_WindFrequency, 0.05) + phase * 6.2831853);
                float fineWave = sin(timeValue * (_WindFrequency * 2.8 + 3.0) + phase * 19.0 + positionOS.y * 7.0);
                float gust = 1.0 + _GlobalGustStrength;
                float amplitude = bend * _WindStrength * _GlobalWindStrength * trunkScale * gust;
                float2 globalDirection = normalize(_GlobalWindDirection.xz + float2(0.0001, 0.0001));
                float2 localDirection = _WindDirection.xz;
                float2 windDirection = normalize(globalDirection + localDirection * 0.1);
                float displacement = slowWave * amplitude + fineWave * _FlutterStrength * amplitude * 0.25;
                float3 windOffset = float3(windDirection.x, 0.0, windDirection.y) * displacement;
                windOffset.y = bend * amplitude * (0.035 + fineWave * 0.015);
                displacedWS = positionWS + windOffset;
                return TransformWorldToObject(displacedWS);
            }

            Varyings FoliageVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                float3 displacedWS;
                float3 positionOS = ApplyFoliageWind(input.positionOS.xyz, vertexInput.positionWS, displacedWS);
                VertexPositionInputs displacedInput = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = displacedInput.positionCS;
                output.positionWS = displacedWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = half4(normalInput.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(displacedInput.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(displacedWS);
                return output;
            }

            half4 FoliageFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(baseSample.a - _Cutoff);
                #endif

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseSample.rgb;
                surfaceData.alpha = baseSample.a;
                half metallic = _Metallic;
                half smoothness = _Smoothness;
                #if defined(_METALLICSPECGLOSSMAP)
                    half4 metallicSample = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
                    metallic *= metallicSample.r;
                    smoothness *= metallicSample.a * _GlossMapScale;
                #endif
                surfaceData.metallic = metallic;
                surfaceData.specular = _SpecColor.rgb;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                #if defined(_NORMALMAP)
                    surfaceData.normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                #endif
                surfaceData.occlusion = 1.0;
                #if defined(_OCCLUSIONMAP)
                    surfaceData.occlusion = lerp(1.0, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g, _OcclusionStrength);
                #endif
                surfaceData.emission = _EmissionColor.rgb;
                #if defined(_EMISSION)
                    surfaceData.emission *= SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                half3 geometricNormal = NormalizeNormalPerPixel(input.normalWS);
                #if defined(_NORMALMAP)
                    half3 tangent = normalize(input.tangentWS.xyz);
                    half3 bitangent = input.tangentWS.w * cross(geometricNormal, tangent);
                    half3x3 tangentToWorld = half3x3(tangent, bitangent, geometricNormal);
                    inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(surfaceData.normalTS, tangentToWorld));
                #else
                    inputData.normalWS = geometricNormal;
                #endif
                inputData.vertexLighting = VertexLighting(input.positionWS, inputData.normalWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _SpecColor;
                float4 _WindDirection;
                float4 _BaseMap_TexelSize;
                float _Cutoff;
                float _AlphaClip;
                float _Metallic;
                float _Smoothness;
                float _GlossMapScale;
                float _BumpScale;
                float _OcclusionStrength;
                float _Cull;
                float _ReceiveShadows;
                float _DisableReceiveShadows;
                float _DisableSpecularHighlights;
                float _DisableEnvironmentReflections;
                float _Surface;
                float _Blend;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _AlphaToMask;
                float _QueueOffset;
                float _WindMode;
                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float _FlutterStrength;
                float _HeightMask;
                float _TrunkStiffness;
                float _WindHeightMin;
                float _WindHeightRange;
                float _WindMaskPower;
            CBUFFER_END
            float4 _GlobalWindDirection;
            float _GlobalWindStrength;
            float _GlobalWindSpeed;
            float _GlobalGustStrength;
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float3 AnimateShadowPosition(float3 positionOS)
            {
                float3 positionWS = TransformObjectToWorld(positionOS);
                float height = saturate((positionOS.y - _WindHeightMin) / max(_WindHeightRange, 0.001));
                float weight = saturate((height - _HeightMask) / max(1.0 - _HeightMask, 0.001));
                float powerValue = _WindMode < 0.5 ? 0.75 : (_WindMode < 1.5 ? 1.4 : 2.2);
                float bend = pow(weight, max(_WindMaskPower, powerValue));
                float trunkScale = _WindMode > 2.5 ? (1.0 - _TrunkStiffness) * 0.12 : 1.0;
                float3 instancePositionWS = TransformObjectToWorld(float3(0, 0, 0));
                float phase = Hash21(instancePositionWS.xz);
                float t = _Time.y * _GlobalWindSpeed * _WindSpeed;
                float slowWave = sin(t * max(_WindFrequency, 0.05) + phase * 6.2831853);
                float fineWave = sin(t * (_WindFrequency * 2.8 + 3.0) + phase * 19.0 + positionOS.y * 7.0);
                float2 direction = normalize(_GlobalWindDirection.xz + _WindDirection.xz * 0.1 + float2(0.0001, 0.0001));
                float amplitude = bend * _WindStrength * _GlobalWindStrength * trunkScale * (1.0 + _GlobalGustStrength);
                positionWS.xz += direction * (slowWave * amplitude + fineWave * _FlutterStrength * amplitude * 0.25);
                positionWS.y += bend * amplitude * (0.035 + fineWave * 0.015);
                return positionWS;
            }

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = AnimateShadowPosition(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _SpecColor;
                float4 _WindDirection;
                float4 _BaseMap_TexelSize;
                float _Cutoff;
                float _AlphaClip;
                float _Metallic;
                float _Smoothness;
                float _GlossMapScale;
                float _BumpScale;
                float _OcclusionStrength;
                float _Cull;
                float _ReceiveShadows;
                float _DisableReceiveShadows;
                float _DisableSpecularHighlights;
                float _DisableEnvironmentReflections;
                float _Surface;
                float _Blend;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _AlphaToMask;
                float _QueueOffset;
                float _WindMode;
                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float _FlutterStrength;
                float _HeightMask;
                float _TrunkStiffness;
                float _WindHeightMin;
                float _WindHeightRange;
                float _WindMaskPower;
            CBUFFER_END
            float4 _GlobalWindDirection;
            float _GlobalWindStrength;
            float _GlobalWindSpeed;
            float _GlobalGustStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float3 AnimateDepthPosition(float3 positionOS)
            {
                float3 positionWS = TransformObjectToWorld(positionOS);
                float height = saturate((positionOS.y - _WindHeightMin) / max(_WindHeightRange, 0.001));
                float weight = saturate((height - _HeightMask) / max(1.0 - _HeightMask, 0.001));
                float powerValue = _WindMode < 0.5 ? 0.75 : (_WindMode < 1.5 ? 1.4 : 2.2);
                float bend = pow(weight, max(_WindMaskPower, powerValue));
                float trunkScale = _WindMode > 2.5 ? (1.0 - _TrunkStiffness) * 0.12 : 1.0;
                float3 instancePositionWS = TransformObjectToWorld(float3(0, 0, 0));
                float phase = Hash21(instancePositionWS.xz);
                float t = _Time.y * _GlobalWindSpeed * _WindSpeed;
                float slowWave = sin(t * max(_WindFrequency, 0.05) + phase * 6.2831853);
                float fineWave = sin(t * (_WindFrequency * 2.8 + 3.0) + phase * 19.0 + positionOS.y * 7.0);
                float2 direction = normalize(_GlobalWindDirection.xz + _WindDirection.xz * 0.1 + float2(0.0001, 0.0001));
                float amplitude = bend * _WindStrength * _GlobalWindStrength * trunkScale * (1.0 + _GlobalGustStrength);
                positionWS.xz += direction * (slowWave * amplitude + fineWave * _FlutterStrength * amplitude * 0.25);
                positionWS.y += bend * amplitude * (0.035 + fineWave * 0.015);
                return positionWS;
            }
            Varyings DepthVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformWorldToHClip(AnimateDepthPosition(input.positionOS.xyz));
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 DepthFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
