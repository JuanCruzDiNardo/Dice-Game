using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class DiceFaceSubmeshResolver
{
    // Matching model --------------------------------------------------------

    private const float MinimumDirectionMatch = 0.5f;

    private readonly struct MatchCandidate
    {
        public MatchCandidate(int anchorIndex, int submeshIndex, float score)
        {
            AnchorIndex = anchorIndex;
            SubmeshIndex = submeshIndex;
            Score = score;
        }

        public int AnchorIndex { get; }
        public int SubmeshIndex { get; }
        public float Score { get; }
    }

    // Public geometric mapping ---------------------------------------------

    public static bool TryResolve(
        MeshRenderer renderer,
        IReadOnlyList<Transform> anchors,
        out Dictionary<Transform, int> submeshByAnchor,
        out string failureReason)
    {
        submeshByAnchor = new Dictionary<Transform, int>();
        failureReason = string.Empty;

        MeshFilter meshFilter = renderer != null
            ? renderer.GetComponent<MeshFilter>()
            : null;
        Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;

        if (mesh == null)
        {
            failureReason = "the dice renderer has no source mesh";
            return false;
        }

        Material[] materials = renderer.sharedMaterials;
        List<int> faceSubmeshes = FindFaceSubmeshes(mesh, materials);

        if (faceSubmeshes.Count < anchors.Count)
        {
            failureReason = $"only {faceSubmeshes.Count} face submeshes were found for {anchors.Count} anchors";
            return false;
        }

        Vector3 meshCenter = mesh.bounds.center;
        Dictionary<int, Vector3> submeshDirections = CalculateSubmeshDirections(
            mesh,
            faceSubmeshes,
            meshCenter);
        List<MatchCandidate> candidates = new();

        for (int anchorIndex = 0; anchorIndex < anchors.Count; anchorIndex++)
        {
            Vector3 anchorPosition = meshFilter.transform.InverseTransformPoint(
                anchors[anchorIndex].position);
            Vector3 anchorDirection = (anchorPosition - meshCenter).normalized;

            if (anchorDirection.sqrMagnitude < Mathf.Epsilon)
            {
                failureReason = $"anchor '{anchors[anchorIndex].name}' is at the mesh center";
                return false;
            }

            foreach (int submeshIndex in faceSubmeshes)
            {
                float score = Vector3.Dot(
                    anchorDirection,
                    submeshDirections[submeshIndex]);
                candidates.Add(new MatchCandidate(anchorIndex, submeshIndex, score));
            }
        }

        candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
        HashSet<int> assignedAnchors = new();
        HashSet<int> assignedSubmeshes = new();

        foreach (MatchCandidate candidate in candidates)
        {
            if (candidate.Score < MinimumDirectionMatch)
                break;

            if (assignedAnchors.Contains(candidate.AnchorIndex) ||
                assignedSubmeshes.Contains(candidate.SubmeshIndex))
            {
                continue;
            }

            assignedAnchors.Add(candidate.AnchorIndex);
            assignedSubmeshes.Add(candidate.SubmeshIndex);
            submeshByAnchor.Add(
                anchors[candidate.AnchorIndex],
                candidate.SubmeshIndex);

            if (assignedAnchors.Count == anchors.Count)
                return true;
        }

        failureReason = "one or more anchors could not be matched to a face submesh";
        submeshByAnchor.Clear();
        return false;
    }

    // Submesh classification and geometry ---------------------------------

    private static List<int> FindFaceSubmeshes(
        Mesh mesh,
        IReadOnlyList<Material> materials)
    {
        List<int> result = new();
        int count = Mathf.Min(mesh.subMeshCount, materials.Count);

        for (int submeshIndex = 0; submeshIndex < count; submeshIndex++)
        {
            Material material = materials[submeshIndex];

            if (material != null && material.name.EndsWith(
                    "Edge",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(submeshIndex);
        }

        return result;
    }

    private static Dictionary<int, Vector3> CalculateSubmeshDirections(
        Mesh mesh,
        IReadOnlyList<int> submeshes,
        Vector3 meshCenter)
    {
        Dictionary<int, Vector3> directions = new();

        using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        Mesh.MeshData meshData = meshDataArray[0];
        using NativeArray<Vector3> positions = new(meshData.vertexCount, Allocator.Temp);
        meshData.GetVertices(positions);

        foreach (int submeshIndex in submeshes)
        {
            SubMeshDescriptor descriptor = meshData.GetSubMesh(submeshIndex);
            Vector3 center = Vector3.zero;

            for (int offset = 0; offset < descriptor.indexCount; offset++)
            {
                int vertexIndex = ReadVertexIndex(
                    meshData,
                    descriptor.indexStart + offset) + descriptor.baseVertex;
                center += positions[vertexIndex];
            }

            center /= Mathf.Max(descriptor.indexCount, 1);
            directions.Add(submeshIndex, (center - meshCenter).normalized);
        }

        return directions;
    }

    private static int ReadVertexIndex(Mesh.MeshData meshData, int index)
    {
        if (meshData.indexFormat == IndexFormat.UInt16)
            return meshData.GetIndexData<ushort>()[index];

        return (int)meshData.GetIndexData<uint>()[index];
    }
}
