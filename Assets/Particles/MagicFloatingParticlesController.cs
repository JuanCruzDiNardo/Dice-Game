using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class MagicFloatingParticlesController : MonoBehaviour
{
    private const string MotesObjectName = "Motes";
    private const string SparksObjectName = "Sparks";
    private const float MoteOutlineSeed = 19f;
    private const float SparkOutlineSeed = 50f;

    [Header("References")]
    [SerializeField]
    [Tooltip("Particle System that renders Particle_1 motes.")]
    private ParticleSystem motes;

    [SerializeField]
    [Tooltip("Particle System that renders Particle_2 sparks.")]
    private ParticleSystem sparks;

    [SerializeField]
    [Tooltip("Shared material that uses the existing HandDrawn2D/Sprite Outline shader.")]
    private Material sharedParticleMaterial;

    [Header("Emission")]
    [SerializeField]
    private MagicParticleEmissionSettings emission = new();

    [Header("Size")]
    [SerializeField]
    private MagicParticleSizeSettings size = new();

    [Header("Lifetime")]
    [SerializeField]
    private MagicParticleLifetimeSettings lifetime = new();

    [Header("Movement")]
    [SerializeField]
    private MagicParticleMotionSettings movement = new();

    [Header("Emission Area")]
    [SerializeField]
    private MagicParticleAreaSettings area = new();

    [Header("Visual")]
    [SerializeField]
    private MagicParticleVisualSettings visual = new();

    [Header("Outline")]
    [SerializeField]
    private MagicParticleOutlineSettings outline = new();

    private MaterialPropertyBlock motesPropertyBlock;
    private MaterialPropertyBlock sparksPropertyBlock;

    public void RefreshEffect()
    {
        ValidateSettings();
        ResolveReferences();

        if (motes == null || sparks == null)
            return;

        float sparkRate = emission.EmissionRate * visual.SparkProportion;
        float moteRate = emission.EmissionRate - sparkRate;
        int sparkCapacity = Mathf.Clamp(
            Mathf.RoundToInt(emission.MaxParticles * visual.SparkProportion),
            0,
            emission.MaxParticles);
        int moteCapacity = emission.MaxParticles - sparkCapacity;

        ParticleSystemRenderer motesRenderer = MagicParticleSystemConfigurator.Configure(
            motes,
            sharedParticleMaterial,
            size,
            lifetime,
            movement,
            area,
            moteRate,
            moteCapacity,
            MagicParticleKind.Mote);

        ParticleSystemRenderer sparksRenderer = MagicParticleSystemConfigurator.Configure(
            sparks,
            sharedParticleMaterial,
            size,
            lifetime,
            movement,
            area,
            sparkRate,
            sparkCapacity,
            MagicParticleKind.Spark);

        motesPropertyBlock ??= new MaterialPropertyBlock();
        sparksPropertyBlock ??= new MaterialPropertyBlock();

        MagicParticleSystemConfigurator.ApplyRendererStyle(
            motesRenderer,
            motesPropertyBlock,
            visual.MoteTexture,
            visual,
            outline,
            MoteOutlineSeed);

        MagicParticleSystemConfigurator.ApplyRendererStyle(
            sparksRenderer,
            sparksPropertyBlock,
            visual.SparkTexture,
            visual,
            outline,
            SparkOutlineSeed);
    }

    public void Initialize(
        ParticleSystem motesSystem,
        ParticleSystem sparksSystem,
        Material particleMaterial,
        Texture2D moteTexture,
        Texture2D sparkTexture)
    {
        motes = motesSystem;
        sparks = sparksSystem;
        sharedParticleMaterial = particleMaterial;
        visual ??= new MagicParticleVisualSettings();
        visual.SetTextures(moteTexture, sparkTexture);
        RefreshEffect();
    }

    public void StopEmission()
    {
        ResolveReferences();
        StopEmitting(motes);
        StopEmitting(sparks);
    }

    [ContextMenu("Refresh Magic Floating Particles")]
    private void RefreshFromContextMenu()
    {
        RefreshEffect();
    }

    private void OnEnable()
    {
        RefreshEffect();
    }

    private void OnValidate()
    {
        RefreshEffect();
    }

    private void Reset()
    {
        ResolveReferences();
        RefreshEffect();
    }

    private void ValidateSettings()
    {
        emission ??= new MagicParticleEmissionSettings();
        size ??= new MagicParticleSizeSettings();
        lifetime ??= new MagicParticleLifetimeSettings();
        movement ??= new MagicParticleMotionSettings();
        area ??= new MagicParticleAreaSettings();
        visual ??= new MagicParticleVisualSettings();
        outline ??= new MagicParticleOutlineSettings();

        emission.Validate();
        size.Validate();
        lifetime.Validate();
        movement.Validate();
        area.Validate();
        visual.Validate();
        outline.Validate();
    }

    private void ResolveReferences()
    {
        motes ??= FindChildParticleSystem(MotesObjectName);
        sparks ??= FindChildParticleSystem(SparksObjectName);

        if (sharedParticleMaterial != null || motes == null)
            return;

        ParticleSystemRenderer renderer = motes.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            sharedParticleMaterial = renderer.sharedMaterial;
    }

    private ParticleSystem FindChildParticleSystem(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<ParticleSystem>() : null;
    }

    private static void StopEmitting(ParticleSystem target)
    {
        if (target == null)
            return;

        target.Stop(
            withChildren: false,
            ParticleSystemStopBehavior.StopEmitting);
    }
}