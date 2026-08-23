using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DiceLabelJitterController : MonoBehaviour
{
    // Naming and shader contract -------------------------------------------

    private const string DefaultLabelName = "FaceLabel";

    private static readonly int JitterFramesPerSecondId =
        Shader.PropertyToID("_DiceLabelJitterFPS");
    private static readonly int JitterStrengthId =
        Shader.PropertyToID("_DiceLabelJitterStrength");
    private static readonly int JitterSeedId =
        Shader.PropertyToID("_DiceLabelJitterSeed");
    private static readonly int JitterVariationCountId =
        Shader.PropertyToID("_DiceLabelVariationCount");
    private static readonly int JitterNoiseScaleId =
        Shader.PropertyToID("_DiceLabelNoiseScale");

    [Header("Label Discovery")]
    [SerializeField]
    [Tooltip("Only TextMeshPro children with this name receive the effect.")]
    private string generatedLabelName = DefaultLabelName;

    [Header("Quantized SDF Contour Jitter")]
    [SerializeField, Min(1f)]
    private float framesPerSecond = 8f;

    [SerializeField, Range(0f, 2f)]
    [Tooltip("Maximum variation of the glyph contour in screen pixels.")]
    private float strengthPixels = 0.8f;

    [SerializeField]
    private int seed = 19;

    [SerializeField, Range(1, 8)]
    private int variationCount = 3;

    [SerializeField, Range(1f, 30f)]
    [Tooltip("Frequency of irregularities along each glyph contour.")]
    private float contourDetailScale = 12f;

    private MaterialPropertyBlock propertyBlock;
    private readonly List<TextMeshPro> labelBuffer = new();

    private string GeneratedLabelName => string.IsNullOrWhiteSpace(generatedLabelName)
        ? DefaultLabelName
        : generatedLabelName.Trim();

    // Unity lifecycle -------------------------------------------------------

    private void OnEnable()
    {
        RefreshJitter();
    }

    private void OnValidate()
    {
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        strengthPixels = Mathf.Clamp(strengthPixels, 0f, 2f);
        variationCount = Mathf.Clamp(variationCount, 1, 8);
        contourDetailScale = Mathf.Clamp(contourDetailScale, 1f, 30f);
        RefreshJitter();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshJitter();
    }

    // Public application API -----------------------------------------------

    public void RefreshJitter()
    {
        propertyBlock ??= new MaterialPropertyBlock();

        labelBuffer.Clear();
        GetComponentsInChildren(true, labelBuffer);

        for (int i = 0; i < labelBuffer.Count; i++)
        {
            TextMeshPro label = labelBuffer[i];

            if (label == null || !string.Equals(
                    label.name,
                    GeneratedLabelName,
                    StringComparison.Ordinal))
            {
                continue;
            }

            ApplyToLabel(label);
        }
    }

    public void ApplyToRenderer(Renderer targetRenderer)
    {
        if (targetRenderer == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.Clear();
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(JitterFramesPerSecondId, framesPerSecond);
        propertyBlock.SetFloat(JitterStrengthId, strengthPixels);
        propertyBlock.SetFloat(JitterSeedId, seed);
        propertyBlock.SetFloat(JitterVariationCountId, variationCount);
        propertyBlock.SetFloat(JitterNoiseScaleId, contourDetailScale);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyToLabel(TextMeshPro label)
    {
        MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();

        if (labelRenderer == null)
            return;

        ApplyToRenderer(labelRenderer);
    }
}
