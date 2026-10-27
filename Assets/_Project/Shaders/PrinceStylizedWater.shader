Shader "Prince/Stylized Water (URP)"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.26,0.58,0.64,1)
        _DeepColor("Deep Color", Color) = (0.08,0.26,0.36,1)
        _Alpha("Alpha", Range(0,1)) = 0.42
        _WaveSpeed("Wave Speed", Range(0,3)) = 0.35
        _WaveScale("Wave Scale", Range(0.1,8)) = 1.2
        _NormalStrength("Normal Strength", Range(0,2)) = 0.35
        _Smoothness("Smoothness", Range(0,1)) = 0.38
        _FresnelPower("Fresnel Power", Range(0.5,8)) = 3
        _HighlightStrength("Highlight Strength", Range(0,1)) = 0.2
        _WaveHeight("Vertex Wave Height", Range(0,0.1)) = 0.012
        [Toggle(_WATER_DEPTH_FADE)] _DepthFadeEnabled("Enable Depth Fade", Float) = 0
        _DepthFadeDistance("Depth Fade Distance", Range(0.01,5)) = 1
        [Toggle(_WATER_FOAM)] _FoamEnabled("Enable Edge Foam", Float) = 0
        _FoamDepth("Foam Depth", Range(0.01,2)) = 0.25
        _FoamColor("Foam Color", Color) = (0.8,0.92,0.92,1)

        [HideInInspector] _SrcBlend("__src", Float) = 5
        [HideInInspector] _DstBlend("__dst", Float) = 10
        [HideInInspector] _ZWrite("__zw", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #pragma shader_feature_local_fragment _WATER_DEPTH_FADE
            #pragma shader_feature_local_fragment _WATER_FOAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _Alpha;
                float _WaveSpeed;
                float _WaveScale;
                float _NormalStrength;
                float _Smoothness;
                float _FresnelPower;
                float _HighlightStrength;
                float _WaveHeight;
                float _DepthFadeEnabled;
                float _DepthFadeDistance;
                float _FoamEnabled;
                float _FoamDepth;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float4 screenPosition : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float eyeDepth : TEXCOORD5;
            };

            float2 NormalizeDirection(float2 value)
            {
                return value * rsqrt(max(dot(value, value), 0.0001));
            }

            float WaveHeight(float2 positionXZ, float timeValue)
            {
                float2 dirA = NormalizeDirection(float2(0.83, 0.55));
                float2 dirB = NormalizeDirection(float2(-0.42, 0.91));
                float phaseA = dot(positionXZ, dirA) * _WaveScale + timeValue * _WaveSpeed;
                float phaseB = dot(positionXZ, dirB) * (_WaveScale * 1.63) - timeValue * (_WaveSpeed * 0.72);
                return (sin(phaseA) + sin(phaseB) * 0.45) * _WaveHeight;
            }

            Varyings WaterVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS.y += WaveHeight(positionWS.xz, _Time.y);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 WaterFragment(Varyings input) : SV_Target
            {
                float2 dirA = NormalizeDirection(float2(0.83, 0.55));
                float2 dirB = NormalizeDirection(float2(-0.42, 0.91));
                float2 p = input.positionWS.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;
                float phaseA = dot(p, dirA) + t;
                float phaseB = dot(p, dirB) * 1.63 - t * 0.72;

                float slopeA = cos(phaseA);
                float slopeB = cos(phaseB) * 0.45;
                float2 gradient = dirA * slopeA + dirB * slopeB * 1.63;
                half3 normalWS = normalize(input.normalWS + half3(-gradient.x, 0, -gradient.y) * _NormalStrength);
                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half nDotV = saturate(dot(normalWS, viewDir));
                half fresnel = pow(1.0h - nDotV, _FresnelPower);

                float ripple = (sin(phaseA) + sin(phaseB) * 0.45) * 0.5 + 0.5;
                half depthBlend = saturate(0.22h + ripple * 0.36h + fresnel * 0.3h);
                half3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthBlend);

                Light mainLight = GetMainLight(input.shadowCoord);
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half lighting = 0.72h + diffuse * mainLight.distanceAttenuation * mainLight.shadowAttenuation * 0.28h;
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half specular = pow(saturate(dot(normalWS, halfDir)), lerp(12.0h, 64.0h, _Smoothness));
                half3 color = baseColor * lighting + mainLight.color * specular * _HighlightStrength * (0.35h + fresnel);
                color = MixFog(color, input.fogFactor);

                half alpha = _Alpha;
                #if defined(_WATER_DEPTH_FADE) || defined(_WATER_FOAM)
                    float2 screenUV = input.screenPosition.xy / max(input.screenPosition.w, 0.0001);
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                    float depthDifference = max(sceneDepth - input.eyeDepth, 0.0);
                    #if defined(_WATER_DEPTH_FADE)
                        alpha *= saturate(depthDifference / max(_DepthFadeDistance, 0.01));
                    #endif
                    #if defined(_WATER_FOAM)
                        half foam = 1.0h - saturate(depthDifference / max(_FoamDepth, 0.01));
                        color = lerp(color, _FoamColor.rgb, foam * _FoamColor.a);
                    #endif
                #endif
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
