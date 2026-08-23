Shader "Hidden/Dice/Hand Drawn Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.045, 0.035, 0.055, 1.0)
        _OutlineWidth("Outline Width (Pixels)", Range(0.5, 6.0)) = 1.5
        _OutlineThreshold("Outline Threshold", Range(0.1, 3.0)) = 0.8
        _NormalThreshold("Normal Threshold", Range(0.01, 1.0)) = 0.28
        _DepthThreshold("Depth Threshold", Range(0.0001, 0.1)) = 0.012
        _OutlineJitterStrength("Outline Jitter Strength (Pixels)", Range(0.0, 3.0)) = 0.75
        _OutlineJitterFPS("Outline Jitter FPS", Range(1.0, 24.0)) = 8.0
        _OutlineSeed("Outline Seed", Float) = 19.0
        _OutlineVariationCount("Outline Variation Count", Range(1.0, 8.0)) = 3.0
        [HideInInspector] _DiceOutlineStencilRef("Dice Outline Stencil Reference", Float) = 8
        [HideInInspector] _DiceOutlineStencilReadMask("Dice Outline Stencil Read Mask", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "HandDrawnScreenOutline"

            Cull Off
            ZWrite Off
            ZTest Always

            Stencil
            {
                Ref [_DiceOutlineStencilRef]
                ReadMask [_DiceOutlineStencilReadMask]
                WriteMask 0
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment OutlineFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "DiceHandDrawnCommon.hlsl"

            // Material parameters ------------------------------------------

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineThreshold;
                float _NormalThreshold;
                float _DepthThreshold;
                float _OutlineJitterStrength;
                float _OutlineJitterFPS;
                float _OutlineSeed;
                float _OutlineVariationCount;
            CBUFFER_END

            // Depth and normal edge detection ------------------------------

            float DiceOutlineEyeDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(saturate(uv));
                return IsPerspectiveProjection()
                    ? LinearEyeDepth(rawDepth, _ZBufferParams)
                    : LinearDepthToEyeDepth(rawDepth);
            }

            float3 DiceSafeNormal(float3 normal)
            {
                return normal * rsqrt(max(dot(normal, normal), 0.0001));
            }

            float DiceOutlinePairResponse(
                float2 centerUV,
                float2 offset,
                float centerDepth,
                float3 centerNormal,
                float viewFacing)
            {
                float2 positiveUV = saturate(centerUV + offset);
                float2 negativeUV = saturate(centerUV - offset);
                float positiveDepth = DiceOutlineEyeDepth(positiveUV);
                float negativeDepth = DiceOutlineEyeDepth(negativeUV);
                float3 positiveNormal = DiceSafeNormal(
                    SampleSceneNormals(positiveUV));
                float3 negativeNormal = DiceSafeNormal(
                    SampleSceneNormals(negativeUV));

                float closestDepth = max(
                    min(centerDepth, min(positiveDepth, negativeDepth)),
                    0.001);

                // A second difference cancels the constant depth slope of a flat
                // polygon viewed at a grazing angle, but remains large at a true
                // silhouette or depth discontinuity.
                float relativeDepthCurvature = abs(
                    positiveDepth + negativeDepth - 2.0 * centerDepth)
                    / closestDepth;

                float normalDifference = max(
                    1.0 - saturate(dot(centerNormal, positiveNormal)),
                    1.0 - saturate(dot(centerNormal, negativeNormal)));

                float grazingAmount = 1.0 - smoothstep(0.12, 0.55, viewFacing);
                float safeDepthThreshold = max(
                    _DepthThreshold * lerp(1.0, 2.5, grazingAmount),
                    0.00001);
                float safeNormalThreshold = min(
                    max(_NormalThreshold * lerp(1.0, 2.5, grazingAmount), 0.0001),
                    0.95);

                float depthResponse = smoothstep(
                    safeDepthThreshold,
                    safeDepthThreshold * 1.75,
                    relativeDepthCurvature);
                float normalResponse = smoothstep(
                    safeNormalThreshold,
                    min(safeNormalThreshold + 0.12, 1.0),
                    normalDifference);
                return max(depthResponse, normalResponse);
            }

            // Full-screen composite ----------------------------------------

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 sourceColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv);

                float drawingVariant = DiceQuantizedVariant(
                    _Time.y,
                    _OutlineJitterFPS,
                    _OutlineVariationCount);

                float2 pixelPosition = uv * _ScaledScreenParams.xy;
                float jitterNoiseX = DiceValueNoise(float3(
                    pixelPosition * 0.035,
                    _OutlineSeed + drawingVariant * 17.0));
                float jitterNoiseY = DiceValueNoise(float3(
                    pixelPosition.yx * 0.031 + 13.0,
                    _OutlineSeed + drawingVariant * 29.0));
                float widthNoise = DiceValueNoise(float3(
                    pixelPosition * 0.021 + 31.0,
                    _OutlineSeed + drawingVariant * 43.0));

                float2 jitterPixels = (float2(jitterNoiseX, jitterNoiseY) * 2.0 - 1.0)
                    * _OutlineJitterStrength;
                float widthVariation = 1.0
                    + (widthNoise * 2.0 - 1.0)
                    * min(_OutlineJitterStrength * 0.18, 0.4);

                float radius = max(_OutlineWidth * widthVariation, 0.5);
                float2 texelSize = _BlitTexture_TexelSize.xy;
                float diagonal = 0.70710678;
                float2 detectionCenterUV = saturate(
                    uv + jitterPixels * texelSize);
                float centerDepth = DiceOutlineEyeDepth(detectionCenterUV);
                float3 centerNormal = DiceSafeNormal(
                    SampleSceneNormals(detectionCenterUV));
                float3 centerNormalVS = TransformWorldToViewDir(
                    centerNormal,
                    true);
                float viewFacing = abs(centerNormalVS.z);
                float edgeResponse = 0.0;

                edgeResponse = max(edgeResponse, DiceOutlinePairResponse(
                    detectionCenterUV,
                    float2(1.0, 0.0) * radius * texelSize,
                    centerDepth, centerNormal, viewFacing));
                edgeResponse = max(edgeResponse, DiceOutlinePairResponse(
                    detectionCenterUV,
                    float2(0.0, 1.0) * radius * texelSize,
                    centerDepth, centerNormal, viewFacing));
                edgeResponse = max(edgeResponse, DiceOutlinePairResponse(
                    detectionCenterUV,
                    float2(diagonal, diagonal) * radius * texelSize,
                    centerDepth, centerNormal, viewFacing));
                edgeResponse = max(edgeResponse, DiceOutlinePairResponse(
                    detectionCenterUV,
                    float2(-diagonal, diagonal) * radius * texelSize,
                    centerDepth, centerNormal, viewFacing));

                half outlineMask = (half)smoothstep(
                    _OutlineThreshold,
                    _OutlineThreshold + 0.25,
                    edgeResponse);

                half blend = outlineMask * _OutlineColor.a;
                half3 result = lerp(sourceColor.rgb, _OutlineColor.rgb, blend);
                return half4(result, sourceColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
