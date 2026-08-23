using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class DiceRuntimeRenderOptimizer : MonoBehaviour
{
    // Shader contract and hard limits --------------------------------------

    public const int MaximumFaceCount = 32;

    private static readonly int BaseMapArrayId = Shader.PropertyToID("_BaseMapArray");
    private static readonly int FaceOverlayMapArrayId = Shader.PropertyToID("_FaceOverlayMapArray");
    private static readonly int FaceCountId = Shader.PropertyToID("_FaceCount");
    private static readonly int FaceBaseTextureSlicesId =
        Shader.PropertyToID("_FaceBaseTextureSlices");
    private static readonly int FaceOverlayTextureSlicesId =
        Shader.PropertyToID("_FaceOverlayTextureSlices");
    private static readonly int FaceOverlayEnabledArrayId =
        Shader.PropertyToID("_FaceOverlayEnabledArray");
    private static readonly int FaceTintsId = Shader.PropertyToID("_FaceTints");

    [Header("Runtime Optimization")]
    [SerializeField]
    private bool optimizeDuringPlay = true;

    [SerializeField, Range(128, 1024)]
    [Tooltip("Resolution used for each unique base or overlay texture slice at runtime.")]
    private int textureResolution = 256;

    [Header("Baked Data")]
    [SerializeField]
    private MeshFilter diceMeshFilter;

    [SerializeField]
    private MeshRenderer diceRenderer;

    [SerializeField]
    private Mesh optimizedMesh;

    [SerializeField]
    private Material faceTemplateMaterial;

    [SerializeField]
    private Material edgeMaterial;

    [SerializeField, HideInInspector]
    private List<string> bakedFaceIds = new();

    [SerializeField, HideInInspector]
    private List<int> bakedMaterialIndices = new();

    private readonly List<Texture2D> baseTextures = new(MaximumFaceCount);
    private readonly List<Texture2D> overlayTextures = new(MaximumFaceCount);
    private readonly float[] baseTextureSlices = new float[MaximumFaceCount];
    private readonly float[] overlayTextureSlices = new float[MaximumFaceCount];
    private readonly float[] overlayEnabled = new float[MaximumFaceCount];
    private readonly Vector4[] faceTints = new Vector4[MaximumFaceCount];
    private MaterialPropertyBlock propertyBlock;
    private Mesh originalMesh;
    private Material[] originalMaterials;
    private Material runtimeFaceMaterial;
    private DiceFaceTextureArrayLease baseTextureArrayLease;
    private DiceFaceTextureArrayLease overlayTextureArrayLease;
    private Texture2D[] lastBaseTextures;
    private Texture2D[] lastOverlayTextures;
    private bool runtimeOptimizationIsActive;
    private bool warnedAboutInvalidSetup;

    // Public state ----------------------------------------------------------

    public bool IsActive => runtimeOptimizationIsActive;

    public bool HasBakedMesh => optimizedMesh != null;

    public int TextureResolution => textureResolution;

    /// <summary>
    /// Returns the original per-face materials while the optimized two-slot
    /// renderer is active. This keeps pause-time face edits deterministic.
    /// </summary>
    internal IReadOnlyList<Material> GetSourceMaterials()
    {
        FindReferencesIfNeeded();

        if (runtimeOptimizationIsActive && originalMaterials != null)
            return originalMaterials;

        return diceRenderer != null
            ? diceRenderer.sharedMaterials
            : Array.Empty<Material>();
    }

#if UNITY_EDITOR
    // Editor bake integration ----------------------------------------------

    public void ConfigureBakedData(
        MeshFilter meshFilter,
        MeshRenderer meshRenderer,
        Mesh newOptimizedMesh,
        Material newFaceTemplateMaterial,
        Material newEdgeMaterial,
        IReadOnlyList<string> faceIds,
        IReadOnlyList<int> faceMaterialIndices)
    {
        diceMeshFilter = meshFilter;
        diceRenderer = meshRenderer;
        optimizedMesh = newOptimizedMesh;
        faceTemplateMaterial = newFaceTemplateMaterial;
        edgeMaterial = newEdgeMaterial;
        bakedFaceIds.Clear();
        bakedMaterialIndices.Clear();

        for (int i = 0; i < faceIds.Count; i++)
        {
            bakedFaceIds.Add(faceIds[i]);
            bakedMaterialIndices.Add(faceMaterialIndices[i]);
        }
    }
#endif

    // Unity lifecycle -------------------------------------------------------

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (TryGetComponent(out DiceVisualController visuals))
            visuals.ApplyFaceAppearanceChanges();
    }

    private void OnDisable()
    {
        bool restoredOriginalRenderer = runtimeOptimizationIsActive;
        RestoreOriginalRenderer();

        // Disabling the optimization component is a supported fallback. Restore
        // the original per-face property blocks immediately instead of waiting
        // for another inspector or gameplay refresh.
        if (restoredOriginalRenderer &&
            Application.isPlaying &&
            gameObject.activeInHierarchy &&
            TryGetComponent(out DiceVisualController visuals) &&
            visuals.isActiveAndEnabled)
        {
            visuals.InvalidateSetupCache();
            visuals.ApplyFaceAppearanceChanges();
        }
    }

    private void OnDestroy()
    {
        ReleaseRuntimeResources();
    }

    private void OnValidate()
    {
        textureResolution = Mathf.ClosestPowerOfTwo(
            Mathf.Clamp(textureResolution, 128, 1024));
        FindReferencesIfNeeded();
    }

    public bool TryApply(IReadOnlyList<DiceFaceRenderData> faces)
    {
        if (!Application.isPlaying || !optimizeDuringPlay || !isActiveAndEnabled)
        {
            RestoreOriginalRenderer();
            return false;
        }

        FindReferencesIfNeeded();

        if (!ValidateBakedSetup(faces))
        {
            RestoreOriginalRenderer();
            return false;
        }

        bool activatedThisCall = EnsureOptimizedRenderer();
        EnsureTextureArrays(faces);
        ApplyPerFaceProperties(faces);

        if (activatedThisCall && TryGetComponent(out DiceStyleController style))
            style.RefreshStyle();

        return true;
    }

    // Setup validation ------------------------------------------------------

    private void FindReferencesIfNeeded()
    {
        if (diceRenderer == null)
            diceRenderer = DiceRendererResolver.FindPrimary(transform);

        if (diceMeshFilter == null && diceRenderer != null)
            diceMeshFilter = diceRenderer.GetComponent<MeshFilter>();
    }

    private bool ValidateBakedSetup(IReadOnlyList<DiceFaceRenderData> faces)
    {
        bool valid = diceRenderer != null &&
                     diceMeshFilter != null &&
                     optimizedMesh != null &&
                     faceTemplateMaterial != null &&
                     edgeMaterial != null &&
                     faces.Count > 0 &&
                     faces.Count <= MaximumFaceCount &&
                     bakedFaceIds.Count == faces.Count &&
                     bakedMaterialIndices.Count == faces.Count;

        if (valid)
        {
            for (int i = 0; i < faces.Count; i++)
            {
                if (!string.Equals(
                        bakedFaceIds[i],
                        faces[i].FaceId,
                        StringComparison.OrdinalIgnoreCase) ||
                    bakedMaterialIndices[i] != faces[i].MaterialIndex)
                {
                    valid = false;
                    break;
                }
            }
        }

        if (!valid && !warnedAboutInvalidSetup)
        {
            Debug.LogWarning(
                $"[{nameof(DiceRuntimeRenderOptimizer)}] {name} has no valid " +
                "optimized mesh for its current face layout. The original " +
                "per-face renderer will be used until the mesh is baked again.",
                this);
            warnedAboutInvalidSetup = true;
        }

        return valid;
    }

    private bool EnsureOptimizedRenderer()
    {
        if (runtimeOptimizationIsActive)
            return false;

        originalMesh = diceMeshFilter.sharedMesh;
        originalMaterials = diceRenderer.sharedMaterials;

        for (int i = 0; i < originalMaterials.Length; i++)
            diceRenderer.SetPropertyBlock(null, i);

        runtimeFaceMaterial = new Material(faceTemplateMaterial)
        {
            name = $"{faceTemplateMaterial.name} (Runtime Optimized)",
            hideFlags = HideFlags.DontSave
        };
        runtimeFaceMaterial.EnableKeyword("_DICE_FACE_ARRAY");

        diceMeshFilter.sharedMesh = optimizedMesh;
        diceRenderer.sharedMaterials = new[]
        {
            runtimeFaceMaterial,
            edgeMaterial
        };
        runtimeOptimizationIsActive = true;
        warnedAboutInvalidSetup = false;
        return true;
    }

    // Dynamic face data -----------------------------------------------------

    private void EnsureTextureArrays(IReadOnlyList<DiceFaceRenderData> faces)
    {
        baseTextures.Clear();
        overlayTextures.Clear();

        for (int i = 0; i < faces.Count; i++)
        {
            baseTextures.Add(
                faces[i].BaseTexture != null
                    ? faces[i].BaseTexture
                    : Texture2D.whiteTexture);
            overlayTextures.Add(
                faces[i].OverlayTexture != null
                    ? faces[i].OverlayTexture
                    : Texture2D.whiteTexture);
        }

        EnsureTextureArray(
            baseTextures,
            ref lastBaseTextures,
            ref baseTextureArrayLease);
        EnsureTextureArray(
            overlayTextures,
            ref lastOverlayTextures,
            ref overlayTextureArrayLease);
    }

    private void EnsureTextureArray(
        IReadOnlyList<Texture2D> textures,
        ref Texture2D[] previousTextures,
        ref DiceFaceTextureArrayLease lease)
    {
        if (ReferencesMatch(previousTextures, textures) && lease != null)
            return;

        DiceFaceTextureArrayCache.Release(lease);
        lease = DiceFaceTextureArrayCache.Acquire(
            textures,
            textureResolution);
        CacheTextureReferences(textures, ref previousTextures);
    }

    private void ApplyPerFaceProperties(IReadOnlyList<DiceFaceRenderData> faces)
    {
        Array.Clear(baseTextureSlices, 0, baseTextureSlices.Length);
        Array.Clear(overlayTextureSlices, 0, overlayTextureSlices.Length);
        Array.Clear(overlayEnabled, 0, overlayEnabled.Length);
        Array.Clear(faceTints, 0, faceTints.Length);

        for (int i = 0; i < faces.Count; i++)
        {
            Texture2D baseTexture = faces[i].BaseTexture != null
                ? faces[i].BaseTexture
                : Texture2D.whiteTexture;
            Texture2D overlayTexture = faces[i].OverlayTexture != null
                ? faces[i].OverlayTexture
                : Texture2D.whiteTexture;

            baseTextureSlices[i] = baseTextureArrayLease.GetSlice(baseTexture);
            overlayTextureSlices[i] = overlayTextureArrayLease.GetSlice(overlayTexture);
            overlayEnabled[i] = faces[i].OverlayTexture != null ? 1f : 0f;
            faceTints[i] = faces[i].Tint;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.Clear();
        diceRenderer.GetPropertyBlock(propertyBlock, 0);
        propertyBlock.SetTexture(BaseMapArrayId, baseTextureArrayLease.TextureArray);
        propertyBlock.SetTexture(
            FaceOverlayMapArrayId,
            overlayTextureArrayLease.TextureArray);
        propertyBlock.SetFloat(FaceCountId, faces.Count);
        propertyBlock.SetFloatArray(FaceBaseTextureSlicesId, baseTextureSlices);
        propertyBlock.SetFloatArray(FaceOverlayTextureSlicesId, overlayTextureSlices);
        propertyBlock.SetFloatArray(FaceOverlayEnabledArrayId, overlayEnabled);
        propertyBlock.SetVectorArray(FaceTintsId, faceTints);
        diceRenderer.SetPropertyBlock(propertyBlock, 0);
    }

    private void RestoreOriginalRenderer()
    {
        if (!runtimeOptimizationIsActive)
            return;

        diceRenderer.SetPropertyBlock(null, 0);
        diceMeshFilter.sharedMesh = originalMesh;
        diceRenderer.sharedMaterials = originalMaterials;
        runtimeOptimizationIsActive = false;
        ReleaseRuntimeResources();
        originalMesh = null;
        originalMaterials = null;
    }

    private void ReleaseRuntimeResources()
    {
        DiceFaceTextureArrayCache.Release(baseTextureArrayLease);
        DiceFaceTextureArrayCache.Release(overlayTextureArrayLease);
        baseTextureArrayLease = null;
        overlayTextureArrayLease = null;
        lastBaseTextures = null;
        lastOverlayTextures = null;

        if (runtimeFaceMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeFaceMaterial);
        else
            DestroyImmediate(runtimeFaceMaterial);

        runtimeFaceMaterial = null;
    }

    private static void CacheTextureReferences(
        IReadOnlyList<Texture2D> textures,
        ref Texture2D[] destination)
    {
        if (destination == null || destination.Length != textures.Count)
            destination = new Texture2D[textures.Count];

        for (int i = 0; i < textures.Count; i++)
            destination[i] = textures[i];
    }

    // Comparison helpers ---------------------------------------------------

    private static bool ReferencesMatch(
        IReadOnlyList<Texture2D> previous,
        IReadOnlyList<Texture2D> current)
    {
        if (previous == null || previous.Count != current.Count)
            return false;

        for (int i = 0; i < current.Count; i++)
        {
            if (previous[i] != current[i])
                return false;
        }

        return true;
    }
}
