using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Public facade for configuring any die that follows the FaceAnchor/Face/Edge
/// conventions. Surface, label and runtime optimization work is delegated to
/// focused collaborators so gameplay code only needs this component.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Hand-Drawn Dice/Dice Visual Controller")]
public sealed class DiceVisualController : MonoBehaviour
{
    // Naming conventions ----------------------------------------------------

    private const string DefaultAnchorPrefix = "FaceAnchor_";
    private const string DefaultMaterialPrefix = "Face_";
    private const string DefaultGeneratedLabelName = "FaceLabel";

    // Serialized configuration ---------------------------------------------

    [Header("References")]
    [SerializeField]
    private MeshRenderer diceRenderer;

    [Header("Mesh Conventions")]
    [SerializeField]
    [Tooltip("Prefix used by child transforms that mark face label positions.")]
    private string faceAnchorPrefix = DefaultAnchorPrefix;

    [SerializeField]
    [Tooltip("Suffix/prefix used to match one material slot to one face identifier.")]
    private string faceMaterialPrefix = DefaultMaterialPrefix;

    [SerializeField]
    [Tooltip("Name used for generated label children.")]
    private string generatedLabelName = DefaultGeneratedLabelName;

    [Header("Face Configuration")]
    [SerializeField]
    private List<DiceFaceData> faces = new();

    [SerializeField]
    [Tooltip("Base texture rendered on every face below optional transparent face overlays.")]
    private Texture2D defaultFaceTexture;

    [Header("Label Settings")]
    [SerializeField]
    [Tooltip("Optional font shared by faces that do not define their own override.")]
    private TMP_FontAsset defaultLabelFont;

    [SerializeField]
    private Vector3 labelLocalRotation = new(90f, 0f, 0f);

    [SerializeField]
    private Vector2 labelRectSize = new(1.5f, 1.5f);

    // Runtime caches --------------------------------------------------------

    private readonly Dictionary<string, DiceFaceData> faceLookup =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DiceFaceRenderData> renderDataBuffer = new();
    private DiceFaceAppearanceService appearanceService;
    private DiceFaceLabelService labelService;
    private DiceRuntimeRenderOptimizer renderOptimizer;
    private DiceRuntimeLabelOptimizer labelOptimizer;
    private bool setupCacheIsValid;

    // Public state ----------------------------------------------------------

    public List<DiceFaceData> Faces => faces;

    private string FaceAnchorPrefix => NormalizeName(
        faceAnchorPrefix,
        DefaultAnchorPrefix);

    private string FaceMaterialPrefix => NormalizeName(
        faceMaterialPrefix,
        DefaultMaterialPrefix);

    private string GeneratedLabelName => NormalizeName(
        generatedLabelName,
        DefaultGeneratedLabelName);

    // Unity lifecycle -------------------------------------------------------

    private void OnEnable()
    {
        InvalidateSetupCache();
        RefreshVisuals();
    }

    private void OnValidate()
    {
        faces ??= new List<DiceFaceData>();
        InvalidateSetupCache();

        if (!Application.isPlaying && isActiveAndEnabled)
            RefreshVisuals();
    }

    // One-time generic setup ------------------------------------------------

    public void AutoSetup()
    {
        FindRendererIfNeeded();

        if (diceRenderer == null)
        {
            Debug.LogError(
                $"[{nameof(DiceVisualController)}] No MeshRenderer was found on {name}.",
                this);
            return;
        }

        List<Transform> anchors = DiceFaceSetupUtility.FindAnchors(
            transform,
            FaceAnchorPrefix);

        if (anchors.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(DiceVisualController)}] No anchors starting with " +
                $"'{FaceAnchorPrefix}' were found.",
                this);
            return;
        }

        Material[] materials = diceRenderer.sharedMaterials;
        bool geometryMappingIsValid = DiceFaceSubmeshResolver.TryResolve(
            diceRenderer,
            anchors,
            out Dictionary<Transform, int> submeshByAnchor,
            out string mappingFailureReason);

        if (geometryMappingIsValid)
        {
            materials = DiceFaceSetupUtility.ReorderFaceMaterials(
                materials,
                anchors,
                submeshByAnchor,
                FaceAnchorPrefix,
                FaceMaterialPrefix);
            diceRenderer.sharedMaterials = materials;
        }
        else
        {
            Debug.LogWarning(
                $"[{nameof(DiceVisualController)}] Could not map faces from " +
                $"FaceAnchors ({mappingFailureReason}). Material names will be used as fallback.",
                this);
        }

        Dictionary<string, DiceFaceData> existingFaces = BuildExistingFaceMap();
        List<DiceFaceData> rebuiltFaces = new(anchors.Count);

        for (int i = 0; i < anchors.Count; i++)
        {
            Transform anchor = anchors[i];
            string faceId = DiceFaceSetupUtility.ExtractFaceId(
                anchor.name,
                FaceAnchorPrefix);

            if (string.IsNullOrEmpty(faceId))
                continue;

            int materialIndex = geometryMappingIsValid
                ? submeshByAnchor[anchor]
                : DiceFaceSetupUtility.FindMaterialIndex(
                    materials,
                    FaceMaterialPrefix + faceId);

            if (materialIndex < 0)
            {
                Debug.LogWarning(
                    $"[{nameof(DiceVisualController)}] Could not find material " +
                    $"'{FaceMaterialPrefix}{faceId}' for anchor '{anchor.name}'.",
                    anchor);
            }

            if (!existingFaces.TryGetValue(faceId, out DiceFaceData face))
                face = new DiceFaceData();

            face.SetInternalReferences(faceId, materialIndex, anchor);
            rebuiltFaces.Add(face);
        }

        faces = rebuiltFaces;
        InvalidateSetupCache();
        RefreshVisuals();

        Debug.Log(
            $"[{nameof(DiceVisualController)}] Configured {faces.Count} faces on {name}.",
            this);
    }

    // Refresh API -----------------------------------------------------------

    public void RefreshVisuals()
    {
        EnsureSetupCache();

        if (diceRenderer == null)
            return;

        ApplyFaceAppearanceChangesInternal();
        ApplyLabelChangesInternal();
    }

    public void ApplyFaceAppearanceChanges()
    {
        EnsureSetupCache();

        if (diceRenderer != null)
            ApplyFaceAppearanceChangesInternal();
    }

    public void ApplyLabelChanges()
    {
        EnsureSetupCache();

        if (diceRenderer != null)
            ApplyLabelChangesInternal();
    }

    public void CopyFaceRenderData(List<DiceFaceRenderData> destination)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        EnsureSetupCache();
        destination.Clear();

        if (appearanceService == null)
            return;

        appearanceService.BuildRenderData(faces, renderDataBuffer);
        destination.AddRange(renderDataBuffer);
    }

    // Per-face mutation API -------------------------------------------------

    public bool SetFaceTexture(
        string faceId,
        Texture2D texture,
        bool applyImmediately = false)
    {
        return SetFaceOverlayTexture(faceId, texture, applyImmediately);
    }

    public bool SetFaceOverlayTexture(
        string faceId,
        Texture2D texture,
        bool applyImmediately = false)
    {
        if (!TryGetFace(faceId, out DiceFaceData face))
            return false;

        face.SetOverlayTexture(texture);

        if (applyImmediately)
            ApplyFaceAppearanceChanges();

        return true;
    }

    public bool SetFaceTint(
        string faceId,
        Color tint,
        bool applyImmediately = false)
    {
        if (!TryGetFace(faceId, out DiceFaceData face))
            return false;

        face.SetFaceTint(tint);

        if (applyImmediately)
            ApplyFaceAppearanceChanges();

        return true;
    }

    public bool SetFaceAppearance(
        string faceId,
        Texture2D texture,
        Color tint,
        bool applyImmediately = false)
    {
        if (!TryGetFace(faceId, out DiceFaceData face))
            return false;

        face.SetOverlayTexture(texture);
        face.SetFaceTint(tint);

        if (applyImmediately)
            ApplyFaceAppearanceChanges();

        return true;
    }

    public bool SetFaceValue(
        string faceId,
        int value,
        bool applyImmediately = false)
    {
        if (!TryGetFace(faceId, out DiceFaceData face))
            return false;

        face.SetValue(value);

        if (applyImmediately)
            ApplyLabelChanges();

        return true;
    }

    public bool SetFaceCustomLabel(
        string faceId,
        string label,
        bool applyImmediately = false)
    {
        if (!TryGetFace(faceId, out DiceFaceData face))
            return false;

        face.SetCustomLabel(label);

        if (applyImmediately)
            ApplyLabelChanges();

        return true;
    }

    public bool TryGetFace(string faceId, out DiceFaceData face)
    {
        EnsureSetupCache();

        if (string.IsNullOrWhiteSpace(faceId))
        {
            face = null;
            return false;
        }

        return faceLookup.TryGetValue(faceId.Trim(), out face);
    }

    // Generated-label management ------------------------------------------

    public void RemoveGeneratedLabels()
    {
        EnsureSetupCache();
        labelService?.RemoveGeneratedLabels(faces);
    }

    // Cache management ------------------------------------------------------

    public void InvalidateSetupCache()
    {
        setupCacheIsValid = false;
        faceLookup.Clear();
        renderDataBuffer.Clear();
        appearanceService = null;
        labelService = null;
        renderOptimizer = null;
        labelOptimizer = null;
    }

    private void EnsureSetupCache()
    {
        if (setupCacheIsValid)
            return;

        FindRendererIfNeeded();
        faceLookup.Clear();
        renderDataBuffer.Clear();
        renderOptimizer = GetComponent<DiceRuntimeRenderOptimizer>();
        labelOptimizer = GetComponent<DiceRuntimeLabelOptimizer>();

        if (diceRenderer == null)
            return;

        IReadOnlyList<Material> sourceMaterials = renderOptimizer != null
            ? renderOptimizer.GetSourceMaterials()
            : diceRenderer.sharedMaterials;

        appearanceService = new DiceFaceAppearanceService(
            diceRenderer,
            sourceMaterials,
            defaultFaceTexture,
            FaceMaterialPrefix,
            faces);
        labelService = new DiceFaceLabelService(
            GeneratedLabelName,
            defaultLabelFont,
            labelLocalRotation,
            labelRectSize,
            faces);

        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face != null && !string.IsNullOrWhiteSpace(face.FaceId))
                faceLookup[face.FaceId] = face;
        }

        setupCacheIsValid = true;
    }

    // Internal refresh pipeline --------------------------------------------

    private void ApplyFaceAppearanceChangesInternal()
    {
        appearanceService.BuildRenderData(faces, renderDataBuffer);

        if (renderOptimizer != null && renderOptimizer.TryApply(renderDataBuffer))
            return;

        appearanceService.ApplyPropertyBlocks(faces);

        if (TryGetComponent(out DiceStyleController style))
            style.RefreshStyle();
    }

    private void ApplyLabelChangesInternal()
    {
        labelService.Apply(faces);

        if (TryGetComponent(out DiceLabelJitterController labelJitter))
            labelJitter.RefreshJitter();

        if (labelOptimizer != null && labelOptimizer.IsActive)
            labelOptimizer.RefreshOptimizedLabels();
    }

    // Local helpers ---------------------------------------------------------

    private Dictionary<string, DiceFaceData> BuildExistingFaceMap()
    {
        Dictionary<string, DiceFaceData> result =
            new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face != null && !string.IsNullOrWhiteSpace(face.FaceId))
                result[face.FaceId] = face;
        }

        return result;
    }

    private void FindRendererIfNeeded()
    {
        if (diceRenderer == null)
            diceRenderer = DiceRendererResolver.FindPrimary(transform);
    }

    private static string NormalizeName(string configuredName, string fallback)
    {
        return string.IsNullOrWhiteSpace(configuredName)
            ? fallback
            : configuredName.Trim();
    }
}
