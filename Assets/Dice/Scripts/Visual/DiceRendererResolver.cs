using System.Collections.Generic;
using UnityEngine;

public static class DiceRendererResolver
{
    // Unity component discovery runs on the main thread. Reusing these buffers
    // avoids temporary arrays whenever an inspector change invalidates a die.
    private static readonly List<MeshRenderer> RendererBuffer = new();
    private static readonly List<Material> MaterialBuffer = new();

    public static MeshRenderer FindPrimary(Transform root)
    {
        if (root == null)
            return null;

        MeshRenderer bestRenderer = null;
        int bestSubmeshCount = -1;
        int bestMaterialCount = -1;
        RendererBuffer.Clear();
        root.GetComponentsInChildren(true, RendererBuffer);

        for (int i = 0; i < RendererBuffer.Count; i++)
        {
            MeshRenderer candidate = RendererBuffer[i];
            Mesh mesh = candidate.GetComponent<MeshFilter>()?.sharedMesh;

            if (mesh == null)
                continue;

            int submeshCount = mesh.subMeshCount;
            MaterialBuffer.Clear();
            candidate.GetSharedMaterials(MaterialBuffer);
            int materialCount = MaterialBuffer.Count;

            if (submeshCount < bestSubmeshCount ||
                (submeshCount == bestSubmeshCount && materialCount <= bestMaterialCount))
            {
                continue;
            }

            bestRenderer = candidate;
            bestSubmeshCount = submeshCount;
            bestMaterialCount = materialCount;
        }

        RendererBuffer.Clear();
        MaterialBuffer.Clear();

        return bestRenderer;
    }
}
