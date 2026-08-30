using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DeathScreenSceneInstaller
{
    private const string LegacyDeathScenePath = "Assets/Scenes/Death.unity";
    private const string DeathScenePath = "Assets/Death Screen/Death.unity";
    private const string DefaultRetryScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string ControllerMarker = "DeathScreenController";
    private const string DeathScreenPanelName = "Death Screen Panel";
    private const string BlackBackgroundName = "Black Background";
    private const string EventSystemName = "Event System";

    [InitializeOnLoadMethod]
    private static void InstallAfterScriptReload()
    {
        if (Application.isBatchMode)
            return;

        EditorApplication.delayCall += InstallIfNeeded;
    }

    [MenuItem("Tools/Death Screen/Prepare Test Scene")]
    public static void PrepareTestScene()
    {
        string scenePath = EnsureSceneIsInsideDeathScreenFolder();
        Scene scene = FindLoadedDeathScene();
        bool openedByInstaller = !scene.IsValid();

        if (openedByInstaller)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            ConfigureScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, DeathScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save the prepared death scene at '{DeathScenePath}'.");
            }

            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        finally
        {
            if (openedByInstaller && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"Death-screen test scene prepared successfully: {DeathScenePath}");
    }

    private static void InstallIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !NeedsInstallation())
            return;

        try
        {
            PrepareTestScene();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static bool NeedsInstallation()
    {
        string candidatePath = AssetDatabase.LoadAssetAtPath<SceneAsset>(DeathScenePath) != null
            ? DeathScenePath
            : LegacyDeathScenePath;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(candidatePath) == null)
            return false;

        string absolutePath = Path.GetFullPath(candidatePath);
        string serializedScene = File.ReadAllText(absolutePath);
        bool sceneConfigured =
            serializedScene.Contains(ControllerMarker, StringComparison.Ordinal) &&
            serializedScene.Contains($"m_Name: {DeathScreenPanelName}", StringComparison.Ordinal) &&
            serializedScene.Contains($"m_Name: {BlackBackgroundName}", StringComparison.Ordinal) &&
            serializedScene.Contains("m_MethodName: Retry", StringComparison.Ordinal) &&
            serializedScene.Contains("m_MethodName: ExitToMainMenu", StringComparison.Ordinal) &&
            serializedScene.Contains($"m_Name: {EventSystemName}", StringComparison.Ordinal);

        bool buildSettingsConfigured = EditorBuildSettings.scenes.Any(
            entry => entry.enabled &&
                     string.Equals(entry.path, DeathScenePath, StringComparison.Ordinal));

        return !sceneConfigured || !buildSettingsConfigured;
    }

    private static string EnsureSceneIsInsideDeathScreenFolder()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DeathScenePath) != null)
            return DeathScenePath;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyDeathScenePath) == null)
        {
            throw new InvalidOperationException(
                $"Death scene was not found at '{LegacyDeathScenePath}' or '{DeathScenePath}'.");
        }

        Scene loadedScene = SceneManager.GetSceneByPath(LegacyDeathScenePath);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
            EditorSceneManager.SaveScene(loadedScene);

        string moveError = AssetDatabase.MoveAsset(LegacyDeathScenePath, DeathScenePath);
        if (!string.IsNullOrEmpty(moveError))
        {
            throw new InvalidOperationException(
                $"Could not move the Death scene into its feature folder: {moveError}");
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return DeathScenePath;
    }

    private static Scene FindLoadedDeathScene()
    {
        Scene scene = SceneManager.GetSceneByPath(DeathScenePath);
        if (scene.IsValid() && scene.isLoaded)
            return scene;

        scene = SceneManager.GetSceneByPath(LegacyDeathScenePath);
        if (scene.IsValid() && scene.isLoaded)
            return scene;

        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene candidate = SceneManager.GetSceneAt(index);
            if (candidate.IsValid() &&
                candidate.isLoaded &&
                string.Equals(candidate.name, "Death", StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return default;
    }

    private static void ConfigureScene(Scene scene)
    {
        GameObject canvasObject = FindRequiredObject(scene, "Death Canvas");
        GameObject backgroundObject = FindRequiredObject(scene, "Background");
        GameObject shadowObject = FindRequiredObject(scene, "Shadow");
        GameObject mageObject = FindRequiredObject(scene, "Mage");
        GameObject lightObject = FindRequiredObject(scene, "Light");
        GameObject titleObject = FindRequiredObject(scene, "Title");
        GameObject mainActionsObject = FindRequiredObject(scene, "Main Actions");
        GameObject retryObject = FindRequiredObject(scene, "Retry Button");
        GameObject exitObject = FindRequiredObject(scene, "Salir Button");
        GameObject cameraObject = FindRequiredObject(scene, "UI Camera");

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(canvasObject);

        Camera uiCamera = GetRequiredComponent<Camera>(cameraObject);
        ConfigureCamera(uiCamera);
        ConfigureCanvas(canvasObject, uiCamera);
        GameObject panelObject = CreateOrConfigureDeathScreenPanel(canvasObject.transform);
        Transform panelTransform = panelObject.transform;

        MoveUnderPanel(backgroundObject, panelTransform);
        MoveUnderPanel(shadowObject, panelTransform);
        MoveUnderPanel(mageObject, panelTransform);
        MoveUnderPanel(lightObject, panelTransform);
        MoveUnderPanel(titleObject, panelTransform);
        MoveUnderPanel(mainActionsObject, panelTransform);

        CanvasGroup blackOverlay = CreateOrConfigureBlackBackground(
            scene,
            panelTransform);
        CanvasGroup retryGroup = GetOrAddComponent<CanvasGroup>(retryObject);
        CanvasGroup exitGroup = GetOrAddComponent<CanvasGroup>(exitObject);
        CanvasGroup titleGroup = GetOrAddComponent<CanvasGroup>(titleObject);
        CanvasGroup[] artworkGroups =
        {
            GetOrAddComponent<CanvasGroup>(backgroundObject),
            GetOrAddComponent<CanvasGroup>(shadowObject),
            GetOrAddComponent<CanvasGroup>(mageObject),
            GetOrAddComponent<CanvasGroup>(lightObject)
        };

        DeathScreenController controller = MoveControllerToPanel(
            canvasObject,
            panelObject);
        controller.Configure(
            blackOverlay,
            retryGroup,
            exitGroup,
            titleGroup,
            artworkGroups,
            DefaultRetryScenePath,
            MainMenuScenePath);

        ConfigurePersistentButtonAction(
            GetRequiredComponent<Button>(retryObject),
            controller,
            controller.Retry);
        ConfigurePersistentButtonAction(
            GetRequiredComponent<Button>(exitObject),
            controller,
            controller.ExitToMainMenu);

        EnsureEventSystem(scene);
    }

    private static DeathScreenController MoveControllerToPanel(
        GameObject canvasObject,
        GameObject panelObject)
    {
        DeathScreenController panelController =
            panelObject.GetComponent<DeathScreenController>();
        DeathScreenController legacyController =
            canvasObject.GetComponent<DeathScreenController>();
        if (legacyController != null && legacyController != panelController)
            UnityEngine.Object.DestroyImmediate(legacyController);

        return panelController != null
            ? panelController
            : panelObject.AddComponent<DeathScreenController>();
    }

    private static void ConfigurePersistentButtonAction(
        Button button,
        DeathScreenController controller,
        UnityAction action)
    {
        for (int index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
        {
            bool sameTarget = button.onClick.GetPersistentTarget(index) == controller;
            bool sameMethod = string.Equals(
                button.onClick.GetPersistentMethodName(index),
                action.Method.Name,
                StringComparison.Ordinal);

            if (sameTarget && sameMethod)
                UnityEventTools.RemovePersistentListener(button.onClick, index);
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static GameObject CreateOrConfigureDeathScreenPanel(Transform canvasTransform)
    {
        Transform existingPanel = canvasTransform.Find(DeathScreenPanelName);
        GameObject panelObject;

        if (existingPanel != null)
        {
            panelObject = existingPanel.gameObject;
        }
        else
        {
            panelObject = new GameObject(DeathScreenPanelName, typeof(RectTransform));
            panelObject.layer = LayerMask.NameToLayer("UI");
            panelObject.transform.SetParent(canvasTransform, false);
        }

        Stretch(GetRequiredComponent<RectTransform>(panelObject));
        return panelObject;
    }

    private static void MoveUnderPanel(GameObject target, Transform panelTransform)
    {
        if (target.transform.parent != panelTransform)
            target.transform.SetParent(panelTransform, true);
    }

    private static void ConfigureCamera(Camera uiCamera)
    {
        uiCamera.clearFlags = CameraClearFlags.SolidColor;
        uiCamera.backgroundColor = Color.black;
        uiCamera.tag = "MainCamera";
    }

    private static void ConfigureCanvas(GameObject canvasObject, Camera uiCamera)
    {
        Canvas canvas = GetRequiredComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = 1f;
        canvasObject.transform.localScale = Vector3.one;
    }

    private static CanvasGroup CreateOrConfigureBlackBackground(
        Scene scene,
        Transform canvasTransform)
    {
        GameObject blackObject = FindObject(scene, BlackBackgroundName);
        if (blackObject == null)
        {
            blackObject = new GameObject(
                BlackBackgroundName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            blackObject.layer = LayerMask.NameToLayer("UI");
            SceneManager.MoveGameObjectToScene(blackObject, scene);
            blackObject.transform.SetParent(canvasTransform, false);
        }

        RectTransform rectTransform = GetRequiredComponent<RectTransform>(blackObject);
        Stretch(rectTransform);
        rectTransform.SetAsFirstSibling();

        Image image = GetOrAddComponent<Image>(blackObject);
        image.color = Color.black;
        image.raycastTarget = true;

        CanvasGroup group = GetOrAddComponent<CanvasGroup>(blackObject);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem[] eventSystems =
            UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);

        for (int index = 0; index < eventSystems.Length; index++)
        {
            if (eventSystems[index].gameObject.scene == scene)
                return;
        }

        var eventSystemObject = new GameObject(
            EventSystemName,
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int legacyIndex = scenes.FindIndex(
            entry => string.Equals(
                entry.path,
                LegacyDeathScenePath,
                StringComparison.Ordinal));
        int deathIndex = scenes.FindIndex(
            entry => string.Equals(
                entry.path,
                DeathScenePath,
                StringComparison.Ordinal));

        if (deathIndex >= 0)
        {
            scenes[deathIndex] = new EditorBuildSettingsScene(DeathScenePath, true);
        }
        else if (legacyIndex >= 0)
        {
            scenes[legacyIndex] = new EditorBuildSettingsScene(DeathScenePath, true);
        }
        else
        {
            scenes.Add(new EditorBuildSettingsScene(DeathScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject FindRequiredObject(Scene scene, string objectName)
    {
        GameObject target = FindObject(scene, objectName);
        if (target == null)
        {
            throw new InvalidOperationException(
                $"Death scene is missing the required object '{objectName}'.");
        }

        return target;
    }

    private static GameObject FindObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] descendants = roots[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int childIndex = 0; childIndex < descendants.Length; childIndex++)
            {
                if (string.Equals(
                    descendants[childIndex].name,
                    objectName,
                    StringComparison.Ordinal))
                {
                    return descendants[childIndex].gameObject;
                }
            }
        }

        return null;
    }

    private static T GetRequiredComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException(
                $"'{target.name}' is missing required component {typeof(T).Name}.");
        }

        return component;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
