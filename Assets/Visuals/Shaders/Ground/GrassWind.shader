Shader "HandDrawn2D/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)

        [Header(Hand Drawn Outline)]
        _OutlineColor("Outline Color", Color) = (0.045, 0.035, 0.055, 1)
        _OutlineWidth("Outline Width (Pixels)", Range(0.5, 4.0)) = 1.5
        _OutlineJitterStrength("Outline Jitter Strength (Pixels)", Range(0.0, 2.0)) = 0.75
        _OutlineJitterFPS("Outline Jitter FPS", Range(1.0, 24.0)) = 8.0
        _OutlineSeed("Outline Seed", Float) = 19.0
        _OutlineVariationCount("Outline Variation Count", Range(1.0, 8.0)) = 3.0
        _OutlineNoiseScale("Outline Noise Scale", Range(0.005, 0.15)) = 0.035
        _AlphaThreshold("Alpha Threshold", Range(0.001, 0.5)) = 0.05
        _EdgeSoftness("Edge Softness", Range(0.001, 0.5)) = 0.1

        [Header(Optional Fill Wind)]
        _WindStrength("Wind Strength", Range(0.0, 0.08)) = 0.0
        _WindSpeed("Wind Speed", Range(0.0, 10.0)) = 2.0
        _WindScale("Wind Variation", Range(0.0, 10.0)) = 1.0
        _CenterRadius("Fixed Center Radius", Range(0.0, 0.4)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "HandDrawnSprite"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Dice/DiceHandDrawnCommon.hlsl"

            struct SpriteAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SpriteVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineJitterStrength;
                float _OutlineJitterFPS;
                float _OutlineSeed;
                float _OutlineVariationCount;
                float _OutlineNoiseScale;
                float _AlphaThreshold;
                float _EdgeSoftness;
                float _WindStrength;
                float _WindSpeed;
                float _WindScale;
                float _CenterRadius;
            CBUFFER_END

            SpriteVaryings SpriteVertex(SpriteAttributes input)
            {
                SpriteVaryings output = (SpriteVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(
                    input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float2 HandDrawnFillUV(SpriteVaryings input)
            {
                float distanceFromCenter = length(input.uv - 0.5);
                float movementMask = smoothstep(
                    _CenterRadius,
                    0.5,
                    distanceFromCenter);

                float phase = _Time.y * _WindSpeed
                    + input.positionWS.x * _WindScale
                    + input.positionWS.z * (_WindScale * 0.73);

                float2 windOffset = float2(
                    sin(phase),
                    sin(phase * 1.7) * 0.25)
                    * _WindStrength
                    * movementMask;

                return input.uv + windOffset;
            }

            half HandDrawnEdgeMask(
                float2 fillUV,
                float2 uvPerPixel,
                float2 pixelPosition,
                half centerAlpha)
            {
                float drawingVariant = DiceQuantizedVariant(
                    _Time.y,
                    _OutlineJitterFPS,
                    _OutlineVariationCount);

                float noiseX = DiceValueNoise(float3(
                    pixelPosition * _OutlineNoiseScale,
                    _OutlineSeed + drawingVariant * 17.0));
                float noiseY = DiceValueNoise(float3(
                    pixelPosition.yx * (_OutlineNoiseScale * 0.89) + 13.0,
                    _OutlineSeed + drawingVariant * 29.0));

                float2 jitterPixels = (float2(noiseX, noiseY) * 2.0 - 1.0)
                    * _OutlineJitterStrength;
                float2 sampleCenter = fillUV + jitterPixels * uvPerPixel;
                float2 radius = max(
                    uvPerPixel * _OutlineWidth,
                    _MainTex_TexelSize.xy * 0.5);
                float2 diagonal = radius * 0.70710678;

                half neighborAlpha = 1.0h;
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter + float2(radius.x, 0.0)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter - float2(radius.x, 0.0)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter + float2(0.0, radius.y)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter - float2(0.0, radius.y)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter + diagonal).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter - diagonal).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter + float2(-diagonal.x, diagonal.y)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, sampleCenter + float2(diagonal.x, -diagonal.y)).a);

                half opaqueCenter = smoothstep(
                    _AlphaThreshold,
                    _AlphaThreshold + _EdgeSoftness,
                    centerAlpha);
                half transparentNeighbor = 1.0h - smoothstep(
                    _AlphaThreshold,
                    _AlphaThreshold + _EdgeSoftness,
                    neighborAlpha);
                return opaqueCenter * transparentNeighbor;
            }

            half4 SpriteFragment(SpriteVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 fillUV = HandDrawnFillUV(input);
                half4 sprite = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    fillUV);

                half4 tint = _Color * input.color;
                half outputAlpha = sprite.a * tint.a;
                clip(outputAlpha - 0.001h);

                float2 uvPerPixel = float2(
                    length(ddx(input.uv)),
                    length(ddy(input.uv)));
                half edge = HandDrawnEdgeMask(
                    fillUV,
                    uvPerPixel,
                    input.positionCS.xy,
                    sprite.a);

                half3 fillColor = sprite.rgb * tint.rgb;
                half outlineBlend = edge * _OutlineColor.a;
                half3 finalColor = lerp(
                    fillColor,
                    _OutlineColor.rgb,
                    outlineBlend);
                return half4(finalColor, outputAlpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
