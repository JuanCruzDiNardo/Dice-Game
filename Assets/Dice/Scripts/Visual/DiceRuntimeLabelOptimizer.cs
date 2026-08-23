using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class DiceRuntimeLabelOptimizer : MonoBehaviour
{
    // Naming conventions ----------------------------------------------------

    private const string DefaultLabelName = "FaceLabel";
    private const string CombinedLabelPrefix = "CombinedDiceLabels";

    [SerializeField]
    private bool optimizeDuringPlay = true;

    [SerializeField]
    [Tooltip("Only TextMeshPro children with this name are combined.")]
    private string generatedLabelName = DefaultLabelName;

    private readonly List<MeshRenderer> sourceRenderers = new();
    private readonly List<GameObject> combinedObjects = new();
    private readonly List<TextMeshPro> labelBuffer = new();
    private readonly Dictionary<Material, List<CombineInstance>> groups = new();
    private DiceLabelJitterController jitterController;
    private bool runtimeOptimizationIsActive;

    public bool IsActive => runtimeOptimizationIsActive;

    private string GeneratedLabelName => string.IsNullOrWhiteSpace(generatedLabelName)
        ? DefaultLabelName
        : generatedLabelName.Trim();

    // Unity lifecycle -------------------------------------------------------

    private void OnEnable()
    {
        if (Application.isPlaying)
            RefreshOptimizedLabels();
    }

    private void OnDisable()
    {
        RestoreSourceLabels();
    }

    // Public rebuild API ----------------------------------------------------

    public void RefreshOptimizedLabels()
    {
        if (!Application.isPlaying || !optimizeDuringPlay || !isActiveAndEnabled)
        {
            RestoreSourceLabels();
            return;
        }

        RestoreSourceLabels();
        BuildMaterialGroups();

        foreach (KeyValuePair<Material, List<CombineInstance>> group in groups)
            CreateCombinedRenderer(group.Key, group.Value);

        if (combinedObjects.Count == 0)
            return;

        foreach (MeshRenderer sourceRenderer in sourceRenderers)
            sourceRenderer.enabled = false;

        runtimeOptimizationIsActive = true;
    }

    private void BuildMaterialGroups()
    {
        foreach (List<CombineInstance> instances in groups.Values)
            instances.Clear();

        sourceRenderers.Clear();
        labelBuffer.Clear();
        GetComponentsInChildren(true, labelBuffer);

        Matrix4x4 worldToDice = transform.worldToLocalMatrix;

        for (int i = 0; i < labelBuffer.Count; i++)
        {
            TextMeshPro label = labelBuffer[i];

            if (label == null || label.name != GeneratedLabelName)
                continue;

            label.ForceMeshUpdate(true, false);

            MeshRenderer sourceRenderer = label.GetComponent<MeshRenderer>();
            Mesh sourceMesh = label.mesh;
            Material sourceMaterial = sourceRenderer != null
                ? sourceRenderer.sharedMaterial
                : null;

            if (sourceRenderer == null || sourceMesh == null ||
                sourceMesh.vertexCount == 0 || sourceMaterial == null)
            {
                continue;
            }

            if (!groups.TryGetValue(sourceMaterial, out List<CombineInstance> instances))
            {
                instances = new List<CombineInstance>();
                groups.Add(sourceMaterial, instances);
            }

            instances.Add(new CombineInstance
            {
                mesh = sourceMesh,
                subMeshIndex = 0,
                transform = worldToDice * label.transform.localToWorldMatrix
            });
            sourceRenderers.Add(sourceRenderer);
        }
    }

    // Combined renderer construction --------------------------------------

    private void CreateCombinedRenderer(
        Material material,
        List<CombineInstance> instances)
    {
        if (instances.Count == 0)
            return;

        int vertexCount = 0;

        foreach (CombineInstance instance in instances)
            vertexCount += instance.mesh.vertexCount;

        Mesh combinedMesh = new()
        {
            name = $"{CombinedLabelPrefix} Mesh",
            indexFormat = vertexCount > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16,
            hideFlags = HideFlags.DontSave
        };
        combinedMesh.CombineMeshes(instances.ToArray(), true, true, false);
        combinedMesh.RecalculateBounds();

        GameObject combinedObject = new($"{CombinedLabelPrefix} ({material.name})")
        {
            hideFlags = HideFlags.DontSave
        };
        combinedObject.transform.SetParent(transform, false);

        MeshFilter meshFilter = combinedObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = combinedMesh;

        MeshRenderer meshRenderer = combinedObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        jitterController ??= GetComponent<DiceLabelJitterController>();
        jitterController?.ApplyToRenderer(meshRenderer);
        combinedObjects.Add(combinedObject);
    }

    private void RestoreSourceLabels()
    {
        foreach (MeshRenderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer != null)
                sourceRenderer.enabled = true;
        }

        foreach (GameObject combinedObject in combinedObjects)
        {
            if (combinedObject == null)
                continue;

            combinedObject.SetActive(false);

            MeshFilter meshFilter = combinedObject.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
                DestroyRuntimeObject(meshFilter.sharedMesh);

            DestroyRuntimeObject(combinedObject);
        }

        sourceRenderers.Clear();
        combinedObjects.Clear();
        runtimeOptimizationIsActive = false;
    }

    private static void DestroyRuntimeObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
