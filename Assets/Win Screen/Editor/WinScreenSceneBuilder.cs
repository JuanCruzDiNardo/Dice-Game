using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WinScreenSceneBuilder
{
    private const string FeatureFolder = "Assets/Win Screen";
    private const string PrefabPath = FeatureFolder + "/Win Screen Panel.prefab";
    private const string ScenePath = FeatureFolder + "/Win.unity";
    private const string PlayAgainScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string FontPath = "Assets/Fonts/Dice/Neythal-Regular SDF.asset";
    private const float RevealDuration = 1.1f;

    private static readonly Color InkColor = new(0.035f, 0.03f, 0.025f, 1f);
    private static readonly Color ButtonColor = new(0.82f, 0.67f, 0.39f, 0.98f);
    private static readonly Color HoverColor = new(0.96f, 0.78f, 0.31f, 1f);
    private static readonly Color PressedColor = new(0.20f, 0.57f, 0.39f, 1f);

    [InitializeOnLoadMethod]
    private static void BuildAfterScriptReload()
    {
        if (!Application.isBatchMode)
            EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("Tools/Win Screen/Prepare Test Scene")]
    public static void PrepareTestScene()
    {
        EnsureRequiredAssets();
        GameObject prefab = BuildPanelPrefab();
        BuildScene(prefab);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"Win-screen test scene prepared successfully: {ScenePath}");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating ||
            !NeedsBuild())
        {
            return;
        }

        try
        {
            PrepareTestScene();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static bool NeedsBuild()
    {
        bool hasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        bool hasScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
        bool isInBuildSettings = EditorBuildSettings.scenes.Any(
            scene => scene.enabled &&
                     string.Equals(scene.path, ScenePath, StringComparison.Ordinal));

        return !hasPrefab || !hasScene || !isInBuildSettings;
    }

    private static GameObject BuildPanelPrefab()
    {
        GameObject panel = CreateUiObject("Win Screen Panel", null);

        try
        {
            Stretch(panel.GetComponent<RectTransform>());
            CanvasGroup group = panel.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            WinScreenController controller = panel.AddComponent<WinScreenController>();

            CreateArtwork(panel.transform, "Fondo", "Fondo.png", true, 43f);
            CreateArtwork(panel.transform, "Sombra", "Sombra.png", false, 0f);
            CreateArtwork(panel.transform, "Mago", "Mago.png", true, 59f);
            RawImage dice = CreateArtwork(panel.transform, "Dados", "Dados.png", true, 71f);
            FloatingUIElement floatingDice = dice.gameObject.AddComponent<FloatingUIElement>();
            floatingDice.Configure(13f, 0.52f, 0.25f);
            CreateArtwork(panel.transform, "Luz", "Luz.png", true, 83f);
            CreateArtwork(panel.transform, "Victoria", "Victoria.png", true, 97f);

            GameObject actions = CreateUiObject("Main Actions", panel.transform);
            Stretch(actions.GetComponent<RectTransform>());

            Button exitButton = CreateButton(
                actions.transform,
                "Salir Button",
                "Salir",
                new Vector2(-550f, 20f),
                54f,
                566344700f);
            Button playAgainButton = CreateButton(
                actions.transform,
                "Volver a jugar Button",
                "Volver a jugar",
                new Vector2(550f, 20f),
                50f,
                558623740f);

            controller.Configure(
                group,
                RevealDuration,
                PlayAgainScenePath,
                MainMenuScenePath);
            UnityEventTools.AddPersistentListener(
                exitButton.onClick,
                controller.ExitToMainMenu);
            UnityEventTools.AddPersistentListener(
                playAgainButton.onClick,
                controller.PlayAgain);

            panel.SetActive(false);
            return PrefabUtility.SaveAsPrefabAsset(panel, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panel);
        }
    }

    private static void BuildScene(GameObject panelPrefab)
    {
        NewSceneMode creationMode = Application.isBatchMode
            ? NewSceneMode.Single
            : NewSceneMode.Additive;
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            creationMode);

        try
        {
            Camera uiCamera = CreateCamera(scene);
            Canvas canvas = CreateCanvas(scene, uiCamera);
            CreateEventSystem(scene);

            GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, scene);
            panel.transform.SetParent(canvas.transform, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.SetActive(true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(panel);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save the Win Screen scene at '{ScenePath}'.");
            }
        }
        finally
        {
            if (!Application.isBatchMode && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Camera CreateCamera(Scene scene)
    {
        var cameraObject = new GameObject("UI Camera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        return camera;
    }

    private static Canvas CreateCanvas(Scene scene, Camera uiCamera)
    {
        var canvasObject = new GameObject(
            "Win Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = 1f;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem(Scene scene)
    {
        var eventSystemObject = new GameObject(
            "Event System",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
        eventSystemObject
            .GetComponent<InputSystemUIInputModule>()
            .AssignDefaultActions();
    }

    private static RawImage CreateArtwork(
        Transform parent,
        string objectName,
        string textureFileName,
        bool addOutline,
        float outlineSeed)
    {
        GameObject artworkObject = CreateUiObject(objectName, parent);
        Stretch(artworkObject.GetComponent<RectTransform>());

        RawImage image = artworkObject.AddComponent<RawImage>();
        image.texture = LoadRequiredAsset<Texture2D>($"{FeatureFolder}/{textureFileName}");
        image.color = Color.white;
        image.raycastTarget = false;

        if (addOutline)
        {
            HandDrawnOutlineEffect outline =
                artworkObject.AddComponent<HandDrawnOutlineEffect>();
            outline.Configure(4.5f, InkColor, 8f, 3, outlineSeed);
        }

        return image;
    }

    private static Button CreateButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 position,
        float fontSize,
        float outlineSeed)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(buttonRect, new Vector2(430f, 102f), position);

        Image background = buttonObject.AddComponent<Image>();
        background.color = ButtonColor;
        background.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = ButtonColor,
            highlightedColor = HoverColor,
            pressedColor = PressedColor,
            selectedColor = HoverColor,
            disabledColor = new Color(0.45f, 0.42f, 0.36f, 0.75f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        HandDrawnOutlineEffect outline =
            buttonObject.AddComponent<HandDrawnOutlineEffect>();
        outline.Configure(6f, InkColor, 8f, 3, outlineSeed);
        buttonObject.AddComponent<HandDrawnButtonFeedback>();

        GameObject labelObject = CreateUiObject("Label", buttonObject.transform);
        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.font = LoadRequiredAsset<TMP_FontAsset>(FontPath);
        text.text = label;
        text.fontSize = fontSize;
        text.color = InkColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(42f, 0f, 20f, 0f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        Stretch(text.rectTransform);
        return button;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var target = new GameObject(name, typeof(RectTransform));
        target.layer = LayerMask.NameToLayer("UI");

        if (parent != null)
            target.transform.SetParent(parent, false);

        return target;
    }

    private static void SetCenteredRect(
        RectTransform rectTransform,
        Vector2 size,
        Vector2 position)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int existingIndex = scenes.FindIndex(
            scene => string.Equals(scene.path, ScenePath, StringComparison.Ordinal));

        if (existingIndex >= 0)
            scenes[existingIndex] = new EditorBuildSettingsScene(ScenePath, true);
        else
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureRequiredAssets()
    {
        string[] requiredPaths =
        {
            $"{FeatureFolder}/Fondo.png",
            $"{FeatureFolder}/Sombra.png",
            $"{FeatureFolder}/Mago.png",
            $"{FeatureFolder}/Dados.png",
            $"{FeatureFolder}/Luz.png",
            $"{FeatureFolder}/Victoria.png",
            FontPath
        };

        string[] missingPaths = requiredPaths
            .Where(path => !File.Exists(Path.GetFullPath(path)))
            .ToArray();
        if (missingPaths.Length > 0)
        {
            throw new FileNotFoundException(
                "Win Screen is missing required assets: " +
                string.Join(", ", missingPaths));
        }
    }

    private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidOperationException(
                $"Could not load the required Win Screen asset '{path}'.");
        }

        return asset;
    }
}
