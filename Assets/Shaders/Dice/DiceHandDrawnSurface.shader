Shader "HandDrawn3D/Surface"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Tint", Color) = (1, 1, 1, 1)
        _OutlineScale("Outline Scale", Range(0.1, 1.0)) = 1.0
        [HideInInspector] _FaceOverlayMap("Face Overlay", 2D) = "white" {}
        [HideInInspector] _FaceOverlayEnabled("Face Overlay Enabled", Float) = 0
        [HideInInspector] _BaseMapArray("Face Base Texture Array", 2DArray) = "" {}
        [HideInInspector] _FaceOverlayMapArray("Face Overlay Texture Array", 2DArray) = "" {}
        [HideInInspector] _FaceCount("Optimized Face Count", Float) = 1

        [HideInInspector] _DiceOutlineStencilRef("Dice Outline Stencil Reference", Float) = 8
        [HideInInspector] _DiceOutlineStencilWriteMask("Dice Outline Stencil Write Mask", Float) = 8

        [Header(Boiling Drawing Jitter)]
        _JitterFPS("Jitter FPS", Range(1.0, 24.0)) = 8.0
        _JitterStrength("Jitter Strength (Pixels)", Range(0.0, 3.0)) = 1.0
        _JitterSeed("Jitter Seed", Float) = 11.0
        _JitterVariationCount("Jitter Variation Count", Range(1.0, 8.0)) = 3.0

        [Header(Surface Fill Variation)]
        _SurfaceJitterFPS("Surface Jitter FPS", Range(1.0, 24.0)) = 8.0
        _SurfaceVariationStrength("Surface Variation Strength", Range(0.0, 0.15)) = 0.025
        _SurfaceNoiseScale("Surface Noise Scale", Range(0.05, 20.0)) = 3.5
        _SurfaceSeed("Surface Seed", Float) = 37.0

        [Header(Artificial Toon Light)]
        _LightDirection("Light Direction", Vector) = (0.35, 0.8, -0.45, 0.0)
        _LightThresholds("Light Thresholds (Dark-Mid, Mid-Light)", Vector) = (0.28, 0.68, 0.0, 0.0)
        _MidBrightness("Mid Brightness", Range(0.0, 1.2)) = 0.82
        _DarkBrightness("Dark Brightness", Range(0.0, 1.0)) = 0.62
        _LightSoftness("Band Softness", Range(0.001, 0.25)) = 0.045
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "HandDrawnSurface"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual

            Stencil
            {
                Ref [_DiceOutlineStencilRef]
                ReadMask [_DiceOutlineStencilWriteMask]
                WriteMask [_DiceOutlineStencilWriteMask]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment
            #pragma multi_compile_instancing
            // Runtime-created optimized materials need this variant to survive
            // player-build shader stripping even though no asset enables it.
            #pragma multi_compile_local _ _DICE_FACE_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DiceHandDrawnCommon.hlsl"

            // Vertex data ---------------------------------------------------

            struct SurfaceAttributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 faceData : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SurfaceVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                nointerpolation float faceIndex : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Surface resources and shared style parameters ----------------

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_FaceOverlayMap);
            SAMPLER(sampler_FaceOverlayMap);
            TEXTURE2D_ARRAY(_BaseMapArray);
            SAMPLER(sampler_BaseMapArray);
            TEXTURE2D_ARRAY(_FaceOverlayMapArray);
            SAMPLER(sampler_FaceOverlayMapArray);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _OutlineScale;
                half _FaceOverlayEnabled;
                float _JitterFPS;
                float _JitterStrength;
                float _JitterSeed;
                float _JitterVariationCount;
                float _SurfaceJitterFPS;
                half _SurfaceVariationStrength;
                float _SurfaceNoiseScale;
                float _SurfaceSeed;
                float4 _LightDirection;
                float4 _LightThresholds;
                half _MidBrightness;
                half _DarkBrightness;
                half _LightSoftness;
                float _FaceCount;
                float _FaceBaseTextureSlices[32];
                float _FaceOverlayTextureSlices[32];
                float _FaceOverlayEnabledArray[32];
                half4 _FaceTints[32];
            CBUFFER_END

            // Forward surface pass -----------------------------------------

            SurfaceVaryings SurfaceVertex(SurfaceAttributes input)
            {
                SurfaceVaryings output = (SurfaceVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInputs.normalWS;
                output.faceIndex = input.faceData.x;
                return output;
            }

            half4 SurfaceFragment(SurfaceVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float drawingVariant = DiceQuantizedVariant(
                    _Time.y,
                    _JitterFPS,
                    _JitterVariationCount);

                float3 drawingOffset = DiceVariantOffset(_JitterSeed, drawingVariant);
                float2 uvPerScreenPixel = float2(
                    length(ddx(input.uv)),
                    length(ddy(input.uv)));

                float2 jitteredUV = input.uv
                    + drawingOffset.xy * uvPerScreenPixel * _JitterStrength;

                half4 baseSample;
                half4 overlaySample;
                half overlayEnabled;
                half4 surfaceTint;

                #if defined(_DICE_FACE_ARRAY)
                int faceCount = max((int)round(_FaceCount), 1);
                int faceIndex = clamp((int)round(input.faceIndex), 0, min(faceCount, 32) - 1);
                int baseTextureSlice = max(
                    (int)round(_FaceBaseTextureSlices[faceIndex]),
                    0);
                int overlayTextureSlice = max(
                    (int)round(_FaceOverlayTextureSlices[faceIndex]),
                    0);
                baseSample = SAMPLE_TEXTURE2D_ARRAY(
                    _BaseMapArray,
                    sampler_BaseMapArray,
                    jitteredUV,
                    baseTextureSlice);
                overlaySample = SAMPLE_TEXTURE2D_ARRAY(
                    _FaceOverlayMapArray,
                    sampler_FaceOverlayMapArray,
                    jitteredUV,
                    overlayTextureSlice);
                overlayEnabled = _FaceOverlayEnabledArray[faceIndex];
                surfaceTint = _FaceTints[faceIndex];
                #else
                baseSample = SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    jitteredUV);
                overlaySample = SAMPLE_TEXTURE2D(
                    _FaceOverlayMap,
                    sampler_FaceOverlayMap,
                    jitteredUV);
                overlayEnabled = _FaceOverlayEnabled;
                surfaceTint = _BaseColor;
                #endif

                // Keep each physical polygon visually coherent while it rotates.
                // The complete drawing changes state, but a flat face is not split
                // into noisy light and dark patches near a toon threshold.
                half diffuseJitter = (half)drawingOffset.z
                    * (half)_JitterStrength
                    * 0.008h;

                half lighting = DiceToonLighting(
                    input.normalWS,
                    _LightDirection.xyz,
                    _LightThresholds.xy,
                    _MidBrightness,
                    _DarkBrightness,
                    _LightSoftness,
                    diffuseJitter);

                float surfaceVariant = DiceQuantizedVariant(
                    _Time.y,
                    _SurfaceJitterFPS,
                    _JitterVariationCount);

                half broadVariation = (half)DiceVariantNoise(
                    input.positionOS,
                    _SurfaceNoiseScale,
                    _SurfaceSeed,
                    surfaceVariant);

                half fineVariation = (half)DiceVariantNoise(
                    input.positionOS,
                    _SurfaceNoiseScale * 2.13,
                    _SurfaceSeed + 17.0,
                    surfaceVariant);

                half surfaceNoise = lerp(broadVariation, fineVariation, 0.3h);
                half surfaceVariation = max(
                    0.0h,
                    1.0h + (surfaceNoise * 2.0h - 1.0h)
                        * _SurfaceVariationStrength);

                half overlayAlpha = saturate(overlaySample.a * overlayEnabled);
                half3 layeredColor = lerp(
                    baseSample.rgb,
                    overlaySample.rgb,
                    overlayAlpha);
                half3 color = layeredColor * surfaceTint.rgb;
                color *= lighting * surfaceVariation;
                return half4(max(color, 0.0h), baseSample.a * surfaceTint.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // Keep the same per-material layout as the forward pass so the
            // shader remains compatible with the SRP Batcher.
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _OutlineScale;
                half _FaceOverlayEnabled;
                float _JitterFPS;
                float _JitterStrength;
                float _JitterSeed;
                float _JitterVariationCount;
                float _SurfaceJitterFPS;
                half _SurfaceVariationStrength;
                float _SurfaceNoiseScale;
                float _SurfaceSeed;
                float4 _LightDirection;
                float4 _LightThresholds;
                half _MidBrightness;
                half _DarkBrightness;
                half _LightSoftness;
                float _FaceCount;
                float _FaceBaseTextureSlices[32];
                float _FaceOverlayTextureSlices[32];
                float _FaceOverlayEnabledArray[32];
                half4 _FaceTints[32];
            CBUFFER_END

            // Camera normals pass used by the independent outline ----------

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthNormalsVaryings DepthNormalsVertex(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output = (DepthNormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            void DepthNormalsFragment(
                DepthNormalsVaryings input,
                out half4 outNormalWS : SV_Target0
                #ifdef _WRITE_RENDERING_LAYERS
                    , out uint outRenderingLayers : SV_Target1
                #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);

                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    outNormalWS = half4(packedNormalWS, _OutlineScale);
                #else
                    outNormalWS = half4(normalWS, _OutlineScale);
                #endif

                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }
    }

    Fallback Off
}
