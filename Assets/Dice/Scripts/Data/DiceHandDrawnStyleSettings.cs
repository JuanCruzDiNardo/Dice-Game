using System;
using UnityEngine;

[Serializable]
public sealed class DiceToonLightingSettings
{
    // Serialized values -----------------------------------------------------

    [SerializeField]
    private Vector3 lightDirection = new(0.35f, 0.8f, -0.45f);

    [SerializeField, Range(0f, 1f)]
    private float darkToMidThreshold = 0.28f;

    [SerializeField, Range(0f, 1f)]
    private float midToLightThreshold = 0.68f;

    [SerializeField, Range(0f, 1.2f)]
    private float midBrightness = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float darkBrightness = 0.58f;

    [SerializeField, Range(0.001f, 0.25f)]
    private float bandSoftness = 0.065f;

    // Normalized shader-facing values --------------------------------------

    public Vector3 LightDirection => lightDirection.sqrMagnitude > 0.0001f
        ? lightDirection.normalized
        : Vector3.up;

    public Vector2 Thresholds => new(darkToMidThreshold, midToLightThreshold);
    public float MidBrightness => midBrightness;
    public float DarkBrightness => darkBrightness;
    public float BandSoftness => bandSoftness;

    public void Validate()
    {
        darkToMidThreshold = Mathf.Clamp01(darkToMidThreshold);
        midToLightThreshold = Mathf.Clamp(midToLightThreshold, darkToMidThreshold, 1f);
        midBrightness = Mathf.Clamp(midBrightness, 0f, 1.2f);
        darkBrightness = Mathf.Clamp01(darkBrightness);
        bandSoftness = Mathf.Clamp(bandSoftness, 0.001f, 0.25f);
    }
}

[Serializable]
public sealed class DiceBoilingJitterSettings
{
    // Serialized values -----------------------------------------------------

    [SerializeField, Range(1f, 24f)]
    private float framesPerSecond = 8f;

    [SerializeField, Range(0f, 3f)]
    private float strengthPixels = 1.2f;

    [SerializeField]
    private float seed = 11f;

    [SerializeField, Range(1, 8)]
    private int variationCount = 3;

    // Shader-facing values --------------------------------------------------

    public float FramesPerSecond => framesPerSecond;
    public float StrengthPixels => strengthPixels;
    public float Seed => seed;
    public int VariationCount => variationCount;

    public void Validate()
    {
        framesPerSecond = Mathf.Clamp(framesPerSecond, 1f, 24f);
        strengthPixels = Mathf.Clamp(strengthPixels, 0f, 3f);
        variationCount = Mathf.Clamp(variationCount, 1, 8);
    }
}

[Serializable]
public sealed class DiceSurfaceVariationSettings
{
    // Serialized values -----------------------------------------------------

    [SerializeField, Range(1f, 24f)]
    private float framesPerSecond = 8f;

    [SerializeField, Range(0f, 0.15f)]
    private float strength = 0.04f;

    [SerializeField, Range(0.05f, 20f)]
    private float noiseScale = 3.2f;

    [SerializeField]
    private float seed = 37f;

    // Shader-facing values --------------------------------------------------

    public float FramesPerSecond => framesPerSecond;
    public float Strength => strength;
    public float NoiseScale => noiseScale;
    public float Seed => seed;

    public void Validate()
    {
        framesPerSecond = Mathf.Clamp(framesPerSecond, 1f, 24f);
        strength = Mathf.Clamp(strength, 0f, 0.15f);
        noiseScale = Mathf.Clamp(noiseScale, 0.05f, 20f);
    }
}
