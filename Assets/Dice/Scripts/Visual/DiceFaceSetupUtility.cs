using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared naming and hierarchy conventions used only while configuring a die.
/// </summary>
internal static class DiceFaceSetupUtility
{
    public static List<Transform> FindAnchors(Transform root, string anchorPrefix)
    {
        List<Transform> allTransforms = new();
        List<Transform> anchors = new();
        root.GetComponentsInChildren(true, allTransforms);

        for (int i = 0; i < allTransforms.Count; i++)
        {
            Transform candidate = allTransforms[i];

            if (candidate != root && candidate.name.StartsWith(
                    anchorPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                anchors.Add(candidate);
            }
        }

        anchors.Sort((left, right) =>
            ParseFaceNumber(left.name, anchorPrefix).CompareTo(
                ParseFaceNumber(right.name, anchorPrefix)));
        return anchors;
    }

    public static string ExtractFaceId(string objectName, string anchorPrefix)
    {
        return objectName.StartsWith(anchorPrefix, StringComparison.OrdinalIgnoreCase)
            ? objectName.Substring(anchorPrefix.Length)
            : string.Empty;
    }

    public static int FindMaterialIndex(
        IReadOnlyList<Material> materials,
        string expectedName)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            Material material = materials[i];

            if (material == null)
                continue;

            string cleanName = material.name
                .Replace(" (Instance)", string.Empty)
                .Trim();

            if (string.Equals(cleanName, expectedName, StringComparison.OrdinalIgnoreCase) ||
                cleanName.EndsWith(expectedName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static Material[] ReorderFaceMaterials(
        Material[] sourceMaterials,
        IReadOnlyList<Transform> anchors,
        IReadOnlyDictionary<Transform, int> submeshByAnchor,
        string anchorPrefix,
        string materialPrefix)
    {
        Material[] reordered = (Material[])sourceMaterials.Clone();

        for (int i = 0; i < anchors.Count; i++)
        {
            Transform anchor = anchors[i];
            string faceId = ExtractFaceId(anchor.name, anchorPrefix);
            int sourceIndex = FindMaterialIndex(
                sourceMaterials,
                materialPrefix + faceId);

            if (sourceIndex < 0 ||
                !submeshByAnchor.TryGetValue(anchor, out int targetIndex) ||
                targetIndex < 0 ||
                targetIndex >= reordered.Length)
            {
                return sourceMaterials;
            }

            reordered[targetIndex] = sourceMaterials[sourceIndex];
        }

        return reordered;
    }

    private static int ParseFaceNumber(string objectName, string anchorPrefix)
    {
        string faceId = ExtractFaceId(objectName, anchorPrefix);
        return int.TryParse(faceId, out int number) ? number : int.MaxValue;
    }
}
