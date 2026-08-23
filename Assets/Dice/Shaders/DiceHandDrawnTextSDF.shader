// World-space TextMeshPro SDF shader for quantized hand-drawn contours.
Shader "Dice/Hand Drawn Text SDF"
{
    Properties
    {
        _FaceColor("Face Color", Color) = (1,1,1,1)
        _FaceDilate("Face Dilate", Range(-1,1)) = 0

        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness("Outline Softness", Range(0,1)) = 0

        _UnderlayColor("Border Color", Color) = (0,0,0,.5)
        _UnderlayOffsetX("Border Offset X", Range(-1,1)) = 0
        _UnderlayOffsetY("Border Offset Y", Range(-1,1)) = 0
        _UnderlayDilate("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness("Border Softness", Range(0,1)) = 0

        _WeightNormal("Weight Normal", Float) = 0
        _WeightBold("Weight Bold", Float) = .5

        _ShaderFlags("Flags", Float) = 0
        _ScaleRatioA("Scale Ratio A", Float) = 1
        _ScaleRatioB("Scale Ratio B", Float) = 1
        _ScaleRatioC("Scale Ratio C", Float) = 1

        _MainTex("Font Atlas", 2D) = "white" {}
        _TextureWidth("Texture Width", Float) = 512
        _TextureHeight("Texture Height", Float) = 512
        _GradientScale("Gradient Scale", Float) = 10
        _ScaleX("Scale X", Float) = 1
        _ScaleY("Scale Y", Float) = 1
        _PerspectiveFilter("Perspective Correction", Range(0,1)) = 0
        _Sharpness("Sharpness", Range(-1,1)) = 0.1

        [Header(Quantized Hand Drawn Contour)]
        _DiceLabelJitterFPS("Jitter FPS", Range(1,24)) = 8
        _DiceLabelJitterStrength("Jitter Strength (Pixels)", Range(0,2)) = 0.8
        _DiceLabelJitterSeed("Jitter Seed", Float) = 19
        _DiceLabelVariationCount("Variation Count", Range(1,8)) = 3
        _DiceLabelNoiseScale("Contour Detail Scale", Range(1,30)) = 12

        _VertexOffsetX("Vertex Offset X", Float) = 0
        _VertexOffsetY("Vertex Offset Y", Float) = 0

        _ClipRect("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        _MaskSoftnessX("Mask Softness X", Float) = 0
        _MaskSoftnessY("Mask Softness Y", Float) = 0

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 0
        _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Lighting Off
        Fog { Mode Off }
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex DiceTextVertex
            #pragma fragment DiceTextFragment
            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc"

            // Vertex data ---------------------------------------------------

            struct DiceTextAttributes
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float4 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct DiceTextVaryings
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex : SV_POSITION;
                fixed4 faceColor : COLOR;
                fixed4 outlineColor : COLOR1;
                float4 texcoord0 : TEXCOORD0;
                half4 param : TEXCOORD1;
                half4 mask : TEXCOORD2;
                float2 localPosition : TEXCOORD5;
                #if (UNDERLAY_ON | UNDERLAY_INNER)
                float4 texcoord1 : TEXCOORD3;
                half2 underlayParam : TEXCOORD4;
                #endif
            };

            // Material parameters ------------------------------------------

            float _DiceLabelJitterFPS;
            float _DiceLabelJitterStrength;
            float _DiceLabelJitterSeed;
            float _DiceLabelVariationCount;
            float _DiceLabelNoiseScale;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            // Quantized contour variation ----------------------------------

            float DiceTextVariation()
            {
                float variationCount = max(floor(_DiceLabelVariationCount + 0.5), 1.0);
                float quantizedFrame = floor(_Time.y * max(_DiceLabelJitterFPS, 0.01));
                return fmod(quantizedFrame, variationCount);
            }

            float DiceTextContourOffset(float2 localPosition)
            {
                float variation = DiceTextVariation();
                float seedPhase = _DiceLabelJitterSeed * 0.6180339;
                float2 position = localPosition * _DiceLabelNoiseScale;

                float waveA = sin(dot(position, float2(0.73, 1.17)) + seedPhase + variation * 2.41);
                float waveB = sin(dot(position, float2(-1.31, 0.47)) - seedPhase * 1.37 + variation * 4.13);
                float waveC = sin(position.y * 1.83 + sin(position.x * 0.61 + variation) + seedPhase * 0.43);
                float contour = waveA * 0.50 + waveB * 0.30 + waveC * 0.20;

                return contour * _DiceLabelJitterStrength;
            }

            // TextMeshPro SDF pipeline -------------------------------------

            DiceTextVaryings DiceTextVertex(DiceTextAttributes input)
            {
                DiceTextVaryings output;
                UNITY_INITIALIZE_OUTPUT(DiceTextVaryings, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float bold = step(input.texcoord0.w, 0);
                float4 vertex = input.vertex;
                vertex.x += _VertexOffsetX;
                vertex.y += _VertexOffsetY;
                float4 clipPosition = UnityObjectToClipPos(vertex);

                float2 pixelSize = clipPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(input.texcoord0.w) * _GradientScale * (_Sharpness + 1);

                if (UNITY_MATRIX_P[3][3] == 0)
                {
                    float facing = abs(dot(
                        UnityObjectToWorldNormal(input.normal),
                        normalize(WorldSpaceViewDir(vertex))));
                    scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, facing);
                }

                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;
                float layerScale = scale;

                scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
                float bias = (0.5 - weight) * scale - 0.5;
                float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    input.color.rgb = UIGammaToLinear(input.color.rgb);

                float opacity = input.color.a;
                #if (UNDERLAY_ON | UNDERLAY_INNER)
                opacity = 1.0;
                #endif

                fixed4 faceColor = fixed4(input.color.rgb, opacity) * _FaceColor;
                faceColor.rgb *= faceColor.a;

                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= opacity;
                outlineColor.rgb *= outlineColor.a;
                outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, outline * 2)));

                #if (UNDERLAY_ON | UNDERLAY_INNER)
                layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
                float layerBias = (0.5 - weight) * layerScale - 0.5 -
                    ((_UnderlayDilate * _ScaleRatioC) * 0.5 * layerScale);
                float2 layerOffset = float2(
                    -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth,
                    -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight);
                #endif

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

                output.vertex = clipPosition;
                output.faceColor = faceColor;
                output.outlineColor = outlineColor;
                output.texcoord0 = float4(input.texcoord0.xy, maskUV);
                output.param = half4(scale, bias - outline, bias + outline, bias);
                output.localPosition = vertex.xy;

                const half2 maskSoftness = half2(
                    max(_UIMaskSoftnessX, _MaskSoftnessX),
                    max(_UIMaskSoftnessY, _MaskSoftnessY));
                output.mask = half4(
                    vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * maskSoftness + pixelSize.xy));

                #if (UNDERLAY_ON | UNDERLAY_INNER)
                output.texcoord1 = float4(input.texcoord0.xy + layerOffset, input.color.a, 0);
                output.underlayParam = half2(layerScale, layerBias);
                #endif

                return output;
            }

            fixed4 DiceTextFragment(DiceTextVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half distanceField = tex2D(_MainTex, input.texcoord0.xy).a * input.param.x;
                half contourOffset = (half)DiceTextContourOffset(input.localPosition);
                half4 color = input.faceColor * saturate(
                    distanceField - input.param.w + contourOffset);

                #ifdef OUTLINE_ON
                color = lerp(
                    input.outlineColor,
                    input.faceColor,
                    saturate(distanceField - input.param.z + contourOffset));
                color *= saturate(distanceField - input.param.y + contourOffset);
                #endif

                #if UNDERLAY_ON
                half underlayDistance = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
                color += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) *
                    saturate(underlayDistance - input.underlayParam.y) * (1 - color.a);
                #endif

                #if UNDERLAY_INNER
                half signedDistance = saturate(distanceField - input.param.z + contourOffset);
                half underlayDistance = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
                color += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) *
                    (1 - saturate(underlayDistance - input.underlayParam.y)) *
                    signedDistance * (1 - color.a);
                #endif

                #if UNITY_UI_CLIP_RECT
                half2 mask = saturate(
                    (_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                color *= mask.x * mask.y;
                #endif

                #if (UNDERLAY_ON | UNDERLAY_INNER)
                color *= input.texcoord1.z;
                #endif

                #if UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
