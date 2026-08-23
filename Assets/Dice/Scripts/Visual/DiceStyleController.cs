using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DiceStyleController : MonoBehaviour
{
    // Shader property IDs ---------------------------------------------------

    private static readonly int JitterFramesPerSecondId =
        Shader.PropertyToID("_JitterFPS");

    private static readonly int JitterStrengthId =
        Shader.PropertyToID("_JitterStrength");

    private static readonly int JitterSeedId =
        Shader.PropertyToID("_JitterSeed");

    private static readonly int JitterVariationCountId =
        Shader.PropertyToID("_JitterVariationCount");

    private static readonly int SurfaceJitterFramesPerSecondId =
        Shader.PropertyToID("_SurfaceJitterFPS");

    private static readonly int SurfaceVariationStrengthId =
        Shader.PropertyToID("_SurfaceVariationStrength");

    private static readonly int SurfaceNoiseScaleId =
        Shader.PropertyToID("_SurfaceNoiseScale");

    private static readonly int SurfaceSeedId =
        Shader.PropertyToID("_SurfaceSeed");

    private static readonly int LightDirectionId =
        Shader.PropertyToID("_LightDirection");

    private static readonly int LightThresholdsId =
        Shader.PropertyToID("_LightThresholds");

    private static readonly int MidBrightnessId =
        Shader.PropertyToID("_MidBrightness");

    private static readonly int DarkBrightnessId =
        Shader.PropertyToID("_DarkBrightness");

    private static readonly int LightSoftnessId =
        Shader.PropertyToID("_LightSoftness");

    [Header("References")]
    [SerializeField]
    private MeshRenderer diceRenderer;

    [Header("Boiling / Hand-Drawn Jitter")]
    [SerializeField]
    private DiceBoilingJitterSettings jitter = new();

    [Header("Surface Fill Variation")]
    [SerializeField]
    private DiceSurfaceVariationSettings surfaceVariation = new();

    [Header("Artificial Toon Lighting")]
    [SerializeField]
    private DiceToonLightingSettings lighting = new();

    // Reused buffers --------------------------------------------------------

    private MaterialPropertyBlock propertyBlock;
    private readonly List<Material> materialBuffer = new();

    // Public API ------------------------------------------------------------

    public void RefreshStyle()
    {
        ValidateSettings();
        FindRendererIfNeeded();

        if (diceRenderer == null)
            return;

        ConfigureRenderer(diceRenderer);
        ApplySharedSurfaceStyle();
    }

    [ContextMenu("Refresh Dice Style")]
    private void RefreshStyleFromContextMenu()
    {
        RefreshStyle();
    }

    // Unity lifecycle -------------------------------------------------------

    private void OnEnable()
    {
        RefreshStyle();
    }

    private void OnValidate()
    {
        RefreshStyle();
    }

    private void Reset()
    {
        FindRendererIfNeeded();
        RefreshStyle();
    }

    // Configuration and renderer setup ------------------------------------

    private void ValidateSettings()
    {
        jitter ??= new DiceBoilingJitterSettings();
        surfaceVariation ??= new DiceSurfaceVariationSettings();
        lighting ??= new DiceToonLightingSettings();

        jitter.Validate();
        surfaceVariation.Validate();
        lighting.Validate();
    }

    private void FindRendererIfNeeded()
    {
        if (diceRenderer != null)
            return;

        diceRenderer = DiceRendererResolver.FindPrimary(transform);
    }

    private void ApplySharedSurfaceStyle()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        materialBuffer.Clear();
        diceRenderer.GetSharedMaterials(materialBuffer);

        for (int materialIndex = 0; materialIndex < materialBuffer.Count; materialIndex++)
        {
            if (materialBuffer[materialIndex] == null)
                continue;

            propertyBlock.Clear();
            diceRenderer.GetPropertyBlock(propertyBlock, materialIndex);

            ApplyJitter(propertyBlock);
            ApplySurfaceVariation(propertyBlock);
            ApplyLighting(propertyBlock);

            diceRenderer.SetPropertyBlock(propertyBlock, materialIndex);
        }
    }

    // Property-block writers -----------------------------------------------

    private void ApplyJitter(MaterialPropertyBlock target)
    {
        target.SetFloat(JitterFramesPerSecondId, jitter.FramesPerSecond);
        target.SetFloat(JitterStrengthId, jitter.StrengthPixels);
        target.SetFloat(JitterSeedId, jitter.Seed);
        target.SetFloat(JitterVariationCountId, jitter.VariationCount);
    }

    private void ApplySurfaceVariation(MaterialPropertyBlock target)
    {
        target.SetFloat(
            SurfaceJitterFramesPerSecondId,
            surfaceVariation.FramesPerSecond);
        target.SetFloat(
            SurfaceVariationStrengthId,
            surfaceVariation.Strength);
        target.SetFloat(
            SurfaceNoiseScaleId,
            surfaceVariation.NoiseScale);
        target.SetFloat(SurfaceSeedId, surfaceVariation.Seed);
    }

    private void ApplyLighting(MaterialPropertyBlock target)
    {
        Vector2 thresholds = lighting.Thresholds;

        target.SetVector(LightDirectionId, lighting.LightDirection);
        target.SetVector(
            LightThresholdsId,
            new Vector4(thresholds.x, thresholds.y, 0f, 0f));
        target.SetFloat(MidBrightnessId, lighting.MidBrightness);
        target.SetFloat(DarkBrightnessId, lighting.DarkBrightness);
        target.SetFloat(LightSoftnessId, lighting.BandSoftness);
    }

    private static void ConfigureRenderer(MeshRenderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }
}
