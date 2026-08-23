using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Owns label discovery, creation and presentation for any dice face layout.
/// </summary>
internal sealed class DiceFaceLabelService
{
    private readonly string generatedLabelName;
    private readonly TMP_FontAsset defaultFont;
    private readonly Quaternion localRotation;
    private readonly Vector2 rectSize;
    private readonly Dictionary<DiceFaceData, TextMeshPro> labels = new();

    public DiceFaceLabelService(
        string generatedLabelName,
        TMP_FontAsset defaultFont,
        Vector3 localEulerAngles,
        Vector2 rectSize,
        IReadOnlyList<DiceFaceData> faces)
    {
        this.generatedLabelName = generatedLabelName;
        this.defaultFont = defaultFont;
        localRotation = Quaternion.Euler(localEulerAngles);
        this.rectSize = rectSize;

        CacheExistingLabels(faces);
    }

    // Cache construction ----------------------------------------------------

    private void CacheExistingLabels(IReadOnlyList<DiceFaceData> faces)
    {
        labels.Clear();

        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face?.Anchor == null)
                continue;

            Transform existing = face.Anchor.Find(generatedLabelName);

            if (existing != null && existing.TryGetComponent(out TextMeshPro label))
                labels[face] = label;
        }
    }

    // Label presentation ----------------------------------------------------

    public void Apply(IReadOnlyList<DiceFaceData> faces)
    {
        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face?.Anchor != null)
                Apply(face, GetOrCreate(face));
        }
    }

    private void Apply(DiceFaceData face, TextMeshPro label)
    {
        Transform labelTransform = label.transform;
        labelTransform.localPosition = Vector3.forward * face.TextOffset;
        labelTransform.localRotation = localRotation;
        labelTransform.localScale = Vector3.one;

        label.text = face.DisplayLabel;

        if (face.Font != null)
            label.font = face.Font;
        else if (defaultFont != null)
            label.font = defaultFont;

        label.color = face.TextColor;
        label.fontSize = face.FontSize;
        label.fontStyle = face.FontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = rectSize;

        ConfigureRenderer(label);
    }

    private TextMeshPro GetOrCreate(DiceFaceData face)
    {
        if (labels.TryGetValue(face, out TextMeshPro cached) && cached != null)
            return cached;

        Transform existing = face.Anchor.Find(generatedLabelName);

        if (existing != null && existing.TryGetComponent(out TextMeshPro existingLabel))
        {
            labels[face] = existingLabel;
            return existingLabel;
        }

        GameObject labelObject = new(generatedLabelName);
        labelObject.transform.SetParent(face.Anchor, false);
        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        labels[face] = label;
        return label;
    }

    private static void ConfigureRenderer(TextMeshPro label)
    {
        if (!label.TryGetComponent(out MeshRenderer textRenderer))
            return;

        textRenderer.shadowCastingMode = ShadowCastingMode.Off;
        textRenderer.receiveShadows = false;
        textRenderer.lightProbeUsage = LightProbeUsage.Off;
        textRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    // Generated-object cleanup ---------------------------------------------

    public void RemoveGeneratedLabels(IReadOnlyList<DiceFaceData> faces)
    {
        for (int i = 0; i < faces.Count; i++)
        {
            DiceFaceData face = faces[i];

            if (face?.Anchor == null)
                continue;

            Transform child = face.Anchor.Find(generatedLabelName);

            if (child == null)
                continue;

            if (Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
        }

        labels.Clear();
    }
}
