using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class DiceOutlineUrpInstaller
{
    // Installer conventions ------------------------------------------------

    private const string FeatureName = "Dice Hand Drawn Outline";
    private const string OutlineMaterialPath =
        "Assets/Dice/Materials/M_DiceOutline.mat";

    [MenuItem("Tools/Hand-Drawn Dice/Install Outline In All URP Renderers")]
    public static void InstallInAllRenderers()
    {
        Material outlineMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);

        if (outlineMaterial == null)
        {
            Debug.LogError(
                $"[Hand-Drawn Dice] Outline material was not found at " +
                $"'{OutlineMaterialPath}'.");
            return;
        }

        string[] rendererGuids =
            AssetDatabase.FindAssets("t:UniversalRendererData");
        int configuredCount = 0;

        foreach (string rendererGuid in rendererGuids)
        {
            string rendererPath = AssetDatabase.GUIDToAssetPath(rendererGuid);
            UniversalRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);

            if (rendererData == null)
                continue;

            FullScreenPassRendererFeature feature = rendererData.rendererFeatures
                .OfType<FullScreenPassRendererFeature>()
                .FirstOrDefault(candidate => candidate.name == FeatureName);

            if (feature == null)
                feature = AddFeature(rendererData);

            ConfigureFeature(feature, outlineMaterial);
            rendererData.SetDirty();
            EditorUtility.SetDirty(rendererData);
            configuredCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[Hand-Drawn Dice] Configured the isolated outline in " +
            $"{configuredCount} URP renderer asset(s).");
    }

    // Renderer Feature construction ---------------------------------------

    private static FullScreenPassRendererFeature AddFeature(
        UniversalRendererData rendererData)
    {
        FullScreenPassRendererFeature feature =
            ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();

        feature.name = FeatureName;
        feature.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(feature, rendererData);

        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
            feature,
            out _,
            out long localIdentifier);

        SerializedObject serializedRenderer = new(rendererData);
        SerializedProperty features =
            serializedRenderer.FindProperty("m_RendererFeatures");
        SerializedProperty featureMap =
            serializedRenderer.FindProperty("m_RendererFeatureMap");
        int index = features.arraySize;

        features.InsertArrayElementAtIndex(index);
        features.GetArrayElementAtIndex(index).objectReferenceValue = feature;
        featureMap.InsertArrayElementAtIndex(index);
        featureMap.GetArrayElementAtIndex(index).longValue = localIdentifier;
        serializedRenderer.ApplyModifiedPropertiesWithoutUndo();

        return feature;
    }

    private static void ConfigureFeature(
        FullScreenPassRendererFeature feature,
        Material outlineMaterial)
    {
        feature.SetActive(true);
        feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint
            .BeforeRenderingPostProcessing;
        feature.fetchColorBuffer = true;
        feature.requirements =
            ScriptableRenderPassInput.Depth |
            ScriptableRenderPassInput.Normal;
        feature.passMaterial = outlineMaterial;
        feature.passIndex = 0;
        feature.bindDepthStencilAttachment = true;
        feature.Create();
        EditorUtility.SetDirty(feature);
    }
}
