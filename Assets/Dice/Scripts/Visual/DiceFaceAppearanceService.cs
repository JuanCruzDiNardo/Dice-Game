using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves and applies per-face surface data without owning gameplay state.
/// A controller instance creates this service only when its setup cache changes.
/// </summary>
internal sealed class DiceFaceAppearanceService
{
    // Shader contract shared with the hand-drawn surface shader.
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int FaceOverlayMapId = Shader.PropertyToID("_FaceOverlayMap");
    private static readonly int FaceOverlayEnabledId = Shader.PropertyToID("_FaceOverlayEnabled");

    private readonly MeshRenderer renderer;
    private readonly IReadOnlyList<Material> sourceMaterials;
    private readonly Texture2D defaultBaseTexture;
    private readonly string materialPrefix;
    private readonly Dictionary<DiceFaceData, int> materialIndices = new();
    private MaterialPropertyBlock propertyBlock;

    public DiceFaceAppearanceService(
        MeshRenderer renderer,
        IReadOnlyList<Material> sourceMaterials,
        Texture2D defaultBaseTexture,
        string materialPrefix,
        IReadOnlyList<DiceFaceData> faces)
    {
        this.renderer = renderer;
        this.sourceMaterials = sourceMaterials;
        this.defaultBaseTexture = defaultBaseTexture;
        this.materialPrefix = materialPrefix;

        CacheMaterialIndices(faces);
    }

    // Cache construction ----------------------------------------------------

    private void CacheMaterialIndices(IReadOnlyList<DiceFaceData> faces)
    {
        materialIndices.Clear();

        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face != null)
                materialIndices[face] = ResolveMaterialIndex(face);
        }
    }

    private int ResolveMaterialIndex(DiceFaceData face)
    {
        if (face.MaterialIndex >= 0 && face.MaterialIndex < sourceMaterials.Count)
            return face.MaterialIndex;

        return DiceFaceSetupUtility.FindMaterialIndex(
            sourceMaterials,
            materialPrefix + face.FaceId);
    }

    // Render-data production ------------------------------------------------

    public void BuildRenderData(
        IReadOnlyList<DiceFaceData> faces,
        List<DiceFaceRenderData> destination)
    {
        destination.Clear();

        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face == null || !materialIndices.TryGetValue(face, out int materialIndex))
                continue;

            destination.Add(new DiceFaceRenderData(
                face.FaceId,
                materialIndex,
                ResolveBaseTexture(materialIndex),
                face.OverlayTexture,
                ResolveTint(face, materialIndex)));
        }
    }

    // Edit-mode and non-optimized rendering --------------------------------

    public void ApplyPropertyBlocks(IReadOnlyList<DiceFaceData> faces)
    {
        propertyBlock ??= new MaterialPropertyBlock();

        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face == null ||
                !materialIndices.TryGetValue(face, out int materialIndex) ||
                materialIndex < 0 ||
                materialIndex >= sourceMaterials.Count)
            {
                continue;
            }

            ApplyPropertyBlock(face, materialIndex);
        }
    }

    private void ApplyPropertyBlock(DiceFaceData face, int materialIndex)
    {
        propertyBlock.Clear();
        bool hasOverride = false;

        if (defaultBaseTexture != null)
        {
            propertyBlock.SetTexture(BaseMapId, defaultBaseTexture);
            hasOverride = true;
        }

        if (face.OverlayTexture != null)
        {
            propertyBlock.SetTexture(FaceOverlayMapId, face.OverlayTexture);
            propertyBlock.SetFloat(FaceOverlayEnabledId, 1f);
            hasOverride = true;
        }

        if (UsesTintOverride(face))
        {
            propertyBlock.SetColor(BaseColorId, face.FaceTint);
            hasOverride = true;
        }

        // Clear stale face values first. DiceStyleController restores the shared
        // style properties after every face has been processed.
        renderer.SetPropertyBlock(null, materialIndex);

        if (hasOverride)
            renderer.SetPropertyBlock(propertyBlock, materialIndex);
    }

    // Inheritance rules -----------------------------------------------------

    private Texture2D ResolveBaseTexture(int materialIndex)
    {
        if (defaultBaseTexture != null)
            return defaultBaseTexture;

        Texture2D materialTexture = GetMaterialTexture(materialIndex);

        return materialTexture != null ? materialTexture : Texture2D.whiteTexture;
    }

    private Color ResolveTint(DiceFaceData face, int materialIndex)
    {
        if (UsesTintOverride(face))
            return face.FaceTint;

        Material material = GetMaterial(materialIndex);

        return material != null && material.HasProperty(BaseColorId)
            ? material.GetColor(BaseColorId)
            : Color.white;
    }

    private Texture2D GetMaterialTexture(int materialIndex)
    {
        Material material = GetMaterial(materialIndex);

        return material != null && material.HasProperty(BaseMapId)
            ? material.GetTexture(BaseMapId) as Texture2D
            : null;
    }

    private Material GetMaterial(int materialIndex)
    {
        return materialIndex >= 0 && materialIndex < sourceMaterials.Count
            ? sourceMaterials[materialIndex]
            : null;
    }

    private static bool UsesTintOverride(DiceFaceData face)
    {
        return face.FaceTint != Color.white;
    }
}
