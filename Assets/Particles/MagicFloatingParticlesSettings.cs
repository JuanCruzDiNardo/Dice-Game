using System;
using UnityEngine;

[Serializable]
public sealed class MagicParticleEmissionSettings
{
    [SerializeField, Min(0f)]
    [Tooltip("Total particles emitted per second across Motes and Sparks.")]
    private float emissionRate = 20f;

    [SerializeField, Min(1)]
    [Tooltip("Total live-particle budget shared by both child systems.")]
    private int maxParticles = 100;

    public float EmissionRate => emissionRate;
    public int MaxParticles => maxParticles;

    public void Validate()
    {
        emissionRate = Mathf.Max(0f, emissionRate);
        maxParticles = Mathf.Max(1, maxParticles);
    }
}

[Serializable]
public sealed class MagicParticleSizeSettings
{
    [SerializeField, Min(0.001f)]
    [Tooltip("Smallest particle size in world units.")]
    private float minSize = 0.025f;

    [SerializeField, Min(0.001f)]
    [Tooltip("Largest particle size in world units.")]
    private float maxSize = 0.07f;

    public float MinSize => minSize;
    public float MaxSize => maxSize;

    public void Validate()
    {
        minSize = Mathf.Max(0.001f, minSize);
        maxSize = Mathf.Max(minSize, maxSize);
    }
}

[Serializable]
public sealed class MagicParticleLifetimeSettings
{
    [SerializeField, Min(0.1f)]
    [Tooltip("Shortest particle lifetime in seconds.")]
    private float minLifetime = 2f;

    [SerializeField, Min(0.1f)]
    [Tooltip("Longest particle lifetime in seconds.")]
    private float maxLifetime = 4f;

    public float MinLifetime => minLifetime;
    public float MaxLifetime => maxLifetime;

    public void Validate()
    {
        minLifetime = Mathf.Max(0.1f, minLifetime);
        maxLifetime = Mathf.Max(minLifetime, maxLifetime);
    }
}

[Serializable]
public sealed class MagicParticleMotionSettings
{
    [SerializeField, Min(0f)]
    [Tooltip("Minimum upward speed in world units per second.")]
    private float minVerticalSpeed = 0.04f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum upward speed in world units per second.")]
    private float maxVerticalSpeed = 0.1f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum random X/Z drift. The configured value is applied in both directions.")]
    private float horizontalDrift = 0.02f;

    [SerializeField, Min(0f)]
    [Tooltip("Strength of the native Particle System noise movement.")]
    private float noiseStrength = 0.11f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Spatial frequency of the floating noise.")]
    private float noiseFrequency = 0.55f;

    [SerializeField, Min(0f)]
    [Tooltip("Speed at which the noise field changes over time.")]
    private float noiseScrollSpeed = 0.3f;

    public float MinVerticalSpeed => minVerticalSpeed;
    public float MaxVerticalSpeed => maxVerticalSpeed;
    public float HorizontalDrift => horizontalDrift;
    public float NoiseStrength => noiseStrength;
    public float NoiseFrequency => noiseFrequency;
    public float NoiseScrollSpeed => noiseScrollSpeed;

    public void Validate()
    {
        minVerticalSpeed = Mathf.Max(0f, minVerticalSpeed);
        maxVerticalSpeed = Mathf.Max(minVerticalSpeed, maxVerticalSpeed);
        horizontalDrift = Mathf.Max(0f, horizontalDrift);
        noiseStrength = Mathf.Max(0f, noiseStrength);
        noiseFrequency = Mathf.Max(0.01f, noiseFrequency);
        noiseScrollSpeed = Mathf.Max(0f, noiseScrollSpeed);
    }
}

[Serializable]
public sealed class MagicParticleAreaSettings
{
    [SerializeField]
    [Tooltip("Rectangular emission volume in local X/Y/Z units. Keep Y small for a surface-like area.")]
    private Vector3 emissionArea = new(2f, 0.05f, 2f);

    public Vector3 EmissionArea => emissionArea;

    public void Validate()
    {
        emissionArea = new Vector3(
            Mathf.Max(0.01f, emissionArea.x),
            Mathf.Max(0.001f, emissionArea.y),
            Mathf.Max(0.01f, emissionArea.z));
    }
}

[Serializable]
public sealed class MagicParticleVisualSettings
{
    [SerializeField, ColorUsage(true, true)]
    [Tooltip("HDR tint shared by both particle systems.")]
    private Color particleColor = new(1.2f, 0.72f, 0.12f, 1f);

    [SerializeField]
    [Tooltip("Alpha texture used by the Motes Particle System (Particle_1 by default).")]
    private Texture2D moteTexture;

    [SerializeField]
    [Tooltip("Alpha texture used by the Sparks Particle System (Particle_2 by default).")]
    private Texture2D sparkTexture;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Fraction of the total emission and particle budget assigned to Sparks.")]
    private float sparkProportion = 0.25f;

    public Color ParticleColor => particleColor;
    public Texture2D MoteTexture => moteTexture;
    public Texture2D SparkTexture => sparkTexture;
    public float SparkProportion => sparkProportion;

    public void SetTextures(Texture2D mote, Texture2D spark)
    {
        moteTexture = mote;
        sparkTexture = spark;
    }

    public void Validate()
    {
        sparkProportion = Mathf.Clamp01(sparkProportion);
    }
}

[Serializable]
public sealed class MagicParticleOutlineSettings
{
    [SerializeField, ColorUsage(true, false)]
    [Tooltip("Ink color used around the visible alpha silhouette.")]
    private Color outlineColor = new(0.045f, 0.035f, 0.055f, 0.9f);

    [SerializeField, Range(0.5f, 4f)]
    [Tooltip("Outline thickness in screen pixels.")]
    private float outlineThickness = 1.1f;

    [SerializeField, Range(0f, 2f)]
    [Tooltip("Amplitude in pixels of the hand-drawn contour irregularity.")]
    private float irregularityAmplitude = 0.45f;

    [SerializeField, Range(1f, 24f)]
    [Tooltip("How often the hand-drawn contour switches to a new variation.")]
    private float vibrationFramesPerSecond = 8f;

    public Color OutlineColor => outlineColor;
    public float OutlineThickness => outlineThickness;
    public float IrregularityAmplitude => irregularityAmplitude;
    public float VibrationFramesPerSecond => vibrationFramesPerSecond;

    public void Validate()
    {
        outlineThickness = Mathf.Clamp(outlineThickness, 0.5f, 4f);
        irregularityAmplitude = Mathf.Clamp(irregularityAmplitude, 0f, 2f);
        vibrationFramesPerSecond = Mathf.Clamp(vibrationFramesPerSecond, 1f, 24f);
    }
}
