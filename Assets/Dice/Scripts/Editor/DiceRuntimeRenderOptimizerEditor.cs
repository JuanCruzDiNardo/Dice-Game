using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(DiceRuntimeRenderOptimizer))]
public sealed class DiceRuntimeRenderOptimizerEditor : Editor
{
    // Inspector -------------------------------------------------------------

    private DiceRuntimeRenderOptimizer optimizer;

    private void OnEnable()
    {
        optimizer = (DiceRuntimeRenderOptimizer)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Optimized Mesh", EditorStyles.boldLabel);

        MessageType messageType = optimizer.HasBakedMesh
            ? MessageType.Info
            : MessageType.Warning;
        string message = optimizer.HasBakedMesh
            ? "The runtime face mesh is baked and ready. Re-bake after changing mesh submeshes or face layout."
            : "Bake once after Auto Setup. The FBX is never modified.";
        EditorGUILayout.HelpBox(message, messageType);

        if (GUILayout.Button("Bake Optimized Face Mesh", GUILayout.Height(28)))
            DiceOptimizedMeshBaker.Bake(optimizer);
    }
}

internal static class DiceOptimizedMeshBaker
{
    // Asset conventions -----------------------------------------------------

    private const string GeneratedFolder = "Assets/Dice/Generated";

    public static Mesh Bake(DiceRuntimeRenderOptimizer optimizer)
    {
        DiceVisualController visuals = optimizer.GetComponent<DiceVisualController>();

        if (visuals == null)
            throw new InvalidOperationException("DiceVisualController is required before baking.");

        visuals.InvalidateSetupCache();
        visuals.RefreshVisuals();

        List<DiceFaceRenderData> faces = new();
        visuals.CopyFaceRenderData(faces);

        if (faces.Count == 0)
            throw new InvalidOperationException("No configured faces were found.");

        if (faces.Count > DiceRuntimeRenderOptimizer.MaximumFaceCount)
        {
            throw new InvalidOperationException(
                $"The optimized shader supports up to {DiceRuntimeRenderOptimizer.MaximumFaceCount} faces.");
        }

        MeshRenderer renderer = DiceRendererResolver.FindPrimary(optimizer.transform);

        if (renderer == null)
        {
            throw new InvalidOperationException(
                $"No mesh renderer was found below {visuals.name}.");
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
            throw new InvalidOperationException("The dice renderer has no source MeshFilter.");

        Material[] materials = renderer.sharedMaterials;
        Dictionary<int, int> faceOrdinalBySubmesh = BuildFaceSubmeshMap(faces, materials.Length);
        int edgeSubmesh = FindEdgeSubmesh(materials, faceOrdinalBySubmesh);
        Mesh bakedMesh = BuildOptimizedMesh(
            meshFilter.sharedMesh,
            faceOrdinalBySubmesh,
            edgeSubmesh);
        Mesh savedMesh = SaveMeshAsset(bakedMesh, faces.Count);

        List<string> faceIds = new(faces.Count);
        List<int> faceMaterialIndices = new(faces.Count);

        for (int i = 0; i < faces.Count; i++)
        {
            faceIds.Add(faces[i].FaceId);
            faceMaterialIndices.Add(faces[i].MaterialIndex);
        }

        Undo.RecordObject(optimizer, "Bake Optimized Dice Mesh");
        optimizer.ConfigureBakedData(
            meshFilter,
            renderer,
            savedMesh,
            materials[faces[0].MaterialIndex],
            materials[edgeSubmesh],
            faceIds,
            faceMaterialIndices);
        EditorUtility.SetDirty(optimizer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(optimizer);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[{nameof(DiceRuntimeRenderOptimizer)}] Baked {faces.Count} faces " +
            $"from {meshFilter.sharedMesh.subMeshCount} submeshes into 2 submeshes.",
            optimizer);

        return savedMesh;
    }

    // Bake validation -------------------------------------------------------

    private static Dictionary<int, int> BuildFaceSubmeshMap(
        IReadOnlyList<DiceFaceRenderData> faces,
        int materialCount)
    {
        Dictionary<int, int> faceOrdinalBySubmesh = new();

        for (int faceOrdinal = 0; faceOrdinal < faces.Count; faceOrdinal++)
        {
            int materialIndex = faces[faceOrdinal].MaterialIndex;

            if (materialIndex < 0 || materialIndex >= materialCount)
            {
                throw new InvalidOperationException(
                    $"Face {faces[faceOrdinal].FaceId} is not bound to a valid material slot.");
            }

            if (!faceOrdinalBySubmesh.TryAdd(materialIndex, faceOrdinal))
            {
                throw new InvalidOperationException(
                    $"Multiple faces resolve to material slot {materialIndex}.");
            }
        }

        return faceOrdinalBySubmesh;
    }

    private static int FindEdgeSubmesh(
        IReadOnlyList<Material> materials,
        IReadOnlyDictionary<int, int> faceOrdinalBySubmesh)
    {
        int fallback = -1;

        for (int i = 0; i < materials.Count; i++)
        {
            if (faceOrdinalBySubmesh.ContainsKey(i))
                continue;

            if (fallback >= 0)
            {
                throw new InvalidOperationException(
                    "The optimized layout expects one non-face submesh for Edge.");
            }

            fallback = i;
        }

        if (fallback >= 0)
            return fallback;

        throw new InvalidOperationException("No Edge material slot was found.");
    }

    private static Mesh BuildOptimizedMesh(
        Mesh source,
        IReadOnlyDictionary<int, int> faceOrdinalBySubmesh,
        int edgeSubmesh)
    {
        using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(source);
        Mesh.MeshData meshData = meshDataArray[0];
        int vertexCount = meshData.vertexCount;

        using NativeArray<Vector3> sourcePositions = new(vertexCount, Allocator.Temp);
        meshData.GetVertices(sourcePositions);

        bool hasNormals = source.HasVertexAttribute(VertexAttribute.Normal);
        bool hasTangents = source.HasVertexAttribute(VertexAttribute.Tangent);
        bool hasColors = source.HasVertexAttribute(VertexAttribute.Color);
        bool hasUv0 = source.HasVertexAttribute(VertexAttribute.TexCoord0);
        using NativeArray<Vector3> sourceNormals = new(vertexCount, Allocator.Temp);
        using NativeArray<Vector4> sourceTangents = new(vertexCount, Allocator.Temp);
        using NativeArray<Color32> sourceColors = new(vertexCount, Allocator.Temp);
        using NativeArray<Vector2> sourceUv0 = new(vertexCount, Allocator.Temp);

        if (hasNormals)
            meshData.GetNormals(sourceNormals);

        if (hasTangents)
            meshData.GetTangents(sourceTangents);

        if (hasColors)
            meshData.GetColors(sourceColors);

        if (hasUv0)
            meshData.GetUVs(0, sourceUv0);

        List<Vector3> positions = new();
        List<Vector3> normals = new();
        List<Vector4> tangents = new();
        List<Color32> colors = new();
        List<Vector2> uv0 = new();
        List<Vector2> faceData = new();
        List<int> faceTriangles = new();
        List<int> edgeTriangles = new();

        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            bool isFace = faceOrdinalBySubmesh.TryGetValue(submesh, out int faceOrdinal);

            if (!isFace && submesh != edgeSubmesh)
                continue;

            SubMeshDescriptor descriptor = meshData.GetSubMesh(submesh);

            if (descriptor.topology != MeshTopology.Triangles)
                throw new InvalidOperationException("Only triangle dice meshes are supported.");

            Dictionary<int, int> remappedVertices = new();
            List<int> targetTriangles = isFace ? faceTriangles : edgeTriangles;

            for (int indexOffset = 0; indexOffset < descriptor.indexCount; indexOffset++)
            {
                int originalIndex = ReadVertexIndex(
                    meshData,
                    descriptor.indexStart + indexOffset) + descriptor.baseVertex;

                if (!remappedVertices.TryGetValue(originalIndex, out int optimizedIndex))
                {
                    optimizedIndex = positions.Count;
                    remappedVertices.Add(originalIndex, optimizedIndex);
                    positions.Add(sourcePositions[originalIndex]);

                    if (hasNormals)
                        normals.Add(sourceNormals[originalIndex]);

                    if (hasTangents)
                        tangents.Add(sourceTangents[originalIndex]);

                    if (hasColors)
                        colors.Add(sourceColors[originalIndex]);

                    uv0.Add(hasUv0 ? sourceUv0[originalIndex] : Vector2.zero);
                    faceData.Add(new Vector2(isFace ? faceOrdinal : -1f, 0f));
                }

                targetTriangles.Add(optimizedIndex);
            }
        }

        Mesh optimized = new()
        {
            name = $"{source.name}_OptimizedFaces",
            indexFormat = positions.Count > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16
        };
        optimized.SetVertices(positions);

        if (hasNormals)
            optimized.SetNormals(normals);

        if (hasTangents)
            optimized.SetTangents(tangents);

        if (hasColors)
            optimized.SetColors(colors);

        optimized.SetUVs(0, uv0);
        optimized.SetUVs(1, faceData);
        optimized.subMeshCount = 2;
        optimized.SetTriangles(faceTriangles, 0, false);
        optimized.SetTriangles(edgeTriangles, 1, false);

        if (!hasNormals)
            optimized.RecalculateNormals();

        optimized.RecalculateBounds();
        return optimized;
    }

    // Mesh serialization ----------------------------------------------------

    private static int ReadVertexIndex(Mesh.MeshData meshData, int index)
    {
        if (meshData.indexFormat == IndexFormat.UInt16)
            return meshData.GetIndexData<ushort>()[index];

        return (int)meshData.GetIndexData<uint>()[index];
    }

    private static Mesh SaveMeshAsset(Mesh bakedMesh, int faceCount)
    {
        EnsureGeneratedFolder();
        string safeName = string.Concat(
            bakedMesh.name.Split(Path.GetInvalidFileNameChars()));
        string path = $"{GeneratedFolder}/{safeName}_{faceCount}Faces.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(bakedMesh, path);
            existing = bakedMesh;
        }
        else
        {
            EditorUtility.CopySerialized(bakedMesh, existing);
            UnityEngine.Object.DestroyImmediate(bakedMesh);
            EditorUtility.SetDirty(existing);
        }

        existing.UploadMeshData(true);
        AssetDatabase.SaveAssets();
        return existing;
    }

    private static void EnsureGeneratedFolder()
    {
        if (AssetDatabase.IsValidFolder(GeneratedFolder))
            return;

        AssetDatabase.CreateFolder("Assets/Dice", "Generated");
    }
}
