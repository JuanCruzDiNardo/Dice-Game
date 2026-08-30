using UnityEngine;
using UnityEngine.Rendering;

internal enum MagicParticleKind
{
    Mote,
    Spark
}

internal static class MagicParticleSystemConfigurator
{
    private const float MinimumSystemDuration = 5f;
    private const float MoteRotationRangeDegrees = 15f;
    private const float SparkRotationRangeDegrees = 180f;
    private const float MinimumNoiseStrengthFactor = 0.75f;
    private const int NoiseOctaveCount = 2;
    private const float NoiseOctaveMultiplier = 0.5f;
    private const float NoiseOctaveScale = 2f;
    private const float NoiseRotationAmount = 0.08f;
    private const float NoiseSizeAmount = 0.03f;
    private const float MinimumRenderedViewportSize = 0.001f;
    private const float MaximumRenderedViewportSize = 0.1f;
    private const float SparkSortingFudge = 0.01f;

    private const float FadeInEnd = 0.15f;
    private const float FadeOutStart = 0.7f;

    private const float SharedOutlineNoiseScale = 0.035f;
    private const float SharedOutlineAlphaThreshold = 0.05f;
    private const float SharedOutlineEdgeSoftness = 0.1f;
    private const float SharedOutlineVariationCount = 3f;

    private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
    private static readonly int TintColorId = Shader.PropertyToID("_Color");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int OutlineJitterStrengthId = Shader.PropertyToID("_OutlineJitterStrength");
    private static readonly int OutlineJitterFramesPerSecondId = Shader.PropertyToID("_OutlineJitterFPS");
    private static readonly int OutlineSeedId = Shader.PropertyToID("_OutlineSeed");
    private static readonly int OutlineVariationCountId = Shader.PropertyToID("_OutlineVariationCount");
    private static readonly int OutlineNoiseScaleId = Shader.PropertyToID("_OutlineNoiseScale");
    private static readonly int AlphaThresholdId = Shader.PropertyToID("_AlphaThreshold");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");

    public static ParticleSystemRenderer Configure(
        ParticleSystem target,
        Material sharedMaterial,
        MagicParticleSizeSettings size,
        MagicParticleLifetimeSettings lifetime,
        MagicParticleMotionSettings motion,
        MagicParticleAreaSettings area,
        float emissionRate,
        int maxParticles,
        MagicParticleKind kind)
    {
        ConfigureMainModule(target, size, lifetime, maxParticles, kind);
        ConfigureEmission(target, emissionRate);
        ConfigureShape(target, area);
        ConfigureVelocity(target, motion);
        ConfigureNoise(target, motion);
        ConfigureFade(target);

        ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
        ConfigureRenderer(renderer, sharedMaterial, kind);
        return renderer;
    }

    public static void ApplyRendererStyle(
        ParticleSystemRenderer renderer,
        MaterialPropertyBlock propertyBlock,
        Texture2D texture,
        MagicParticleVisualSettings visual,
        MagicParticleOutlineSettings outline,
        float outlineSeed)
    {
        if (renderer == null || propertyBlock == null)
            return;

        propertyBlock.Clear();
        propertyBlock.SetTexture(MainTextureId, texture != null ? texture : Texture2D.whiteTexture);
        propertyBlock.SetColor(TintColorId, visual.ParticleColor);
        propertyBlock.SetColor(OutlineColorId, outline.OutlineColor);
        propertyBlock.SetFloat(OutlineWidthId, outline.OutlineThickness);
        propertyBlock.SetFloat(OutlineJitterStrengthId, outline.IrregularityAmplitude);
        propertyBlock.SetFloat(OutlineJitterFramesPerSecondId, outline.VibrationFramesPerSecond);
        propertyBlock.SetFloat(OutlineSeedId, outlineSeed);
        propertyBlock.SetFloat(OutlineVariationCountId, SharedOutlineVariationCount);
        propertyBlock.SetFloat(OutlineNoiseScaleId, SharedOutlineNoiseScale);
        propertyBlock.SetFloat(AlphaThresholdId, SharedOutlineAlphaThreshold);
        propertyBlock.SetFloat(EdgeSoftnessId, SharedOutlineEdgeSoftness);
        propertyBlock.SetFloat(WindStrengthId, 0f);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private static void ConfigureMainModule(
        ParticleSystem target,
        MagicParticleSizeSettings size,
        MagicParticleLifetimeSettings lifetime,
        int maxParticles,
        MagicParticleKind kind)
    {
        ParticleSystem.MainModule main = target.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.duration = Mathf.Max(MinimumSystemDuration, lifetime.MaxLifetime);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.gravityModifier = 0f;
        main.startSpeed = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            lifetime.MinLifetime,
            lifetime.MaxLifetime);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(size.MinSize, size.MaxSize);
        main.startColor = Color.white;
        main.maxParticles = Mathf.Max(1, maxParticles);

        float rotationRange = kind == MagicParticleKind.Spark
            ? SparkRotationRangeDegrees
            : MoteRotationRangeDegrees;
        main.startRotation = new ParticleSystem.MinMaxCurve(
            -rotationRange * Mathf.Deg2Rad,
            rotationRange * Mathf.Deg2Rad);
    }

    private static void ConfigureEmission(ParticleSystem target, float emissionRate)
    {
        ParticleSystem.EmissionModule emission = target.emission;
        emission.enabled = emissionRate > 0f;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Max(0f, emissionRate));
        emission.rateOverDistance = 0f;
    }

    private static void ConfigureShape(
        ParticleSystem target,
        MagicParticleAreaSettings area)
    {
        ParticleSystem.ShapeModule shape = target.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = area.EmissionArea;
        shape.randomDirectionAmount = 0f;
    }

    private static void ConfigureVelocity(
        ParticleSystem target,
        MagicParticleMotionSettings motion)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = target.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(
            -motion.HorizontalDrift,
            motion.HorizontalDrift);
        velocity.y = new ParticleSystem.MinMaxCurve(
            motion.MinVerticalSpeed,
            motion.MaxVerticalSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(
            -motion.HorizontalDrift,
            motion.HorizontalDrift);
    }

    private static void ConfigureNoise(
        ParticleSystem target,
        MagicParticleMotionSettings motion)
    {
        ParticleSystem.NoiseModule noise = target.noise;
        noise.enabled = motion.NoiseStrength > 0f;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(
            motion.NoiseStrength * MinimumNoiseStrengthFactor,
            motion.NoiseStrength);
        noise.frequency = motion.NoiseFrequency;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(motion.NoiseScrollSpeed);
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.octaveCount = NoiseOctaveCount;
        noise.octaveMultiplier = NoiseOctaveMultiplier;
        noise.octaveScale = NoiseOctaveScale;
        noise.positionAmount = 1f;
        noise.rotationAmount = NoiseRotationAmount;
        noise.sizeAmount = NoiseSizeAmount;
    }

    private static void ConfigureFade(ParticleSystem target)
    {
        var fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, FadeInEnd),
                new GradientAlphaKey(1f, FadeOutStart),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = target.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);
    }

    private static void ConfigureRenderer(
        ParticleSystemRenderer renderer,
        Material sharedMaterial,
        MagicParticleKind kind)
    {
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sortingFudge = kind == MagicParticleKind.Spark
            ? SparkSortingFudge
            : 0f;
        renderer.sharedMaterial = sharedMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        renderer.minParticleSize = MinimumRenderedViewportSize;
        renderer.maxParticleSize = MaximumRenderedViewportSize;
    }
}
