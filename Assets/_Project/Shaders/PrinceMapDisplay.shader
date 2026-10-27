Shader "Prince/Map Display"
{
    Properties
    {
        [PerRendererData] _MainTex ("Map Texture", 2D) = "white" {}
        _FogTex ("Reveal Texture", 2D) = "white" {}
        _Tint ("Map Tint", Color) = (0.78, 0.66, 0.47, 1)
        _FogColor ("Fog Color", Color) = (0.12, 0.105, 0.085, 1)
        _FogEnabled ("Fog Enabled", Float) = 1
        _CircleClip ("Circle Clip", Float) = 0
        _SepiaStrength ("Sepia Strength", Range(0, 1)) = 0.04
        _TintStrength ("Map Tint Strength", Range(0, 1)) = 0.3
        _FogCenter ("Fog Center", Vector) = (0.5, 0.5, 0, 0)
        _WorldCenter ("World Center", Vector) = (0, 0, 0, 0)
        _WorldRight ("World Right", Vector) = (1, 0, 0, 0)
        _WorldUp ("World Up", Vector) = (0, 0, 1, 0)
        _WorldSpan ("World Span", Float) = 80
        _AreaBounds ("Area Bounds", Vector) = (0, 0, 500, 500)
        _Vignette ("Vignette", Range(0, 1)) = 0.2
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "MapDisplay"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float4 _FogColor;
                float4 _FogCenter;
                float4 _WorldCenter;
                float4 _WorldRight;
                float4 _WorldUp;
                float4 _AreaBounds;
                float _FogEnabled;
                float _CircleClip;
                float _SepiaStrength;
                float _TintStrength;
                float _WorldSpan;
                float _Vignette;
                float4 _ClipRect;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.positionOS;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 centeredUv = input.uv - 0.5;
                if (_CircleClip > 0.5 && dot(centeredUv, centeredUv) > 0.25)
                    discard;

                half4 mapColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 sepia = float3(
                    dot(mapColor.rgb, float3(0.393, 0.769, 0.189)),
                    dot(mapColor.rgb, float3(0.349, 0.686, 0.168)),
                    dot(mapColor.rgb, float3(0.272, 0.534, 0.131)));
                mapColor.rgb = lerp(mapColor.rgb, sepia, saturate(_SepiaStrength));
                mapColor.rgb *= lerp(float3(1.0, 1.0, 1.0), _Tint.rgb, saturate(_TintStrength));

                float2 worldOffset = centeredUv * _WorldSpan;
                float2 worldXZ = _WorldCenter.xz +
                    _WorldRight.xz * worldOffset.x +
                    _WorldUp.xz * worldOffset.y;
                float2 fogUv = (worldXZ - _AreaBounds.xy) / max(_AreaBounds.zw, float2(0.001, 0.001));
                float explored = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, saturate(fogUv)).r;
                float2 areaInside = step(float2(0.0, 0.0), fogUv) * step(fogUv, float2(1.0, 1.0));
                float insideWorld = areaInside.x * areaInside.y;
                float2 vignetteUv = centeredUv * 2.0;
                float vignette = smoothstep(0.38, 1.0, dot(vignetteUv, vignetteUv)) * _Vignette;
                float unseen = (1.0 - saturate(explored)) * saturate(_FogEnabled) * insideWorld;
                mapColor.rgb = lerp(mapColor.rgb, _FogColor.rgb, unseen * 0.88);
                mapColor.rgb = lerp(mapColor.rgb, float3(0.018, 0.021, 0.025), 1.0 - insideWorld);
                mapColor.rgb *= 1.0 - vignette;
                mapColor *= input.color;

                #ifdef UNITY_UI_CLIP_RECT
                    float2 clipped = step(_ClipRect.xy, input.worldPosition.xy) *
                        step(input.worldPosition.xy, _ClipRect.zw);
                    mapColor.a *= clipped.x * clipped.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(mapColor.a - 0.001);
                #endif

                return mapColor;
            }
            ENDHLSL
        }
    }
}
