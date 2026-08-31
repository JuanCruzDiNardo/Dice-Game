using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HandDrawnHealthBar.Editor
{
    public static class HealthBarAssetBuilder
    {
        private const string RootFolder = "Assets/Health Bar";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string FontPath = RootFolder + "/Fonts/Neythal-Regular.ttf";
        private const string PrefabPath = PrefabFolder + "/Hand Drawn Health Bar.prefab";
        private const string TestScenePath = SceneFolder + "/Health Bar Test.unity";

        private static readonly Color OutlineColor =
            new(0.035f, 0.03f, 0.025f, 1f);
        private static readonly Color GoldColor =
            new(0.82f, 0.67f, 0.39f, 0.98f);
        private static readonly Color DarkPanelColor =
            new(0.055f, 0.045f, 0.04f, 0.98f);
        private static readonly Color DemoBackgroundColor =
            new(0.035f, 0.027f, 0.025f, 1f);

        [InitializeOnLoadMethod]
        private static void ScheduleInitialBuild()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("Tools/Health Bar/Rebuild Demo Assets")]
        public static void RebuildDemoAssets()
        {
            BuildAssets(forceRebuild: true);
        }

        private static void BuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            bool prefabExists =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            bool sceneExists =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) != null;

            if (!prefabExists || !sceneExists)
                BuildAssets(forceRebuild: false);
        }

        private static void BuildAssets(bool forceRebuild)
        {
            EnsureAssetFolder(PrefabFolder);
            EnsureAssetFolder(SceneFolder);

            try
            {
                if (forceRebuild ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                {
                    BuildPrefab();
                }

                if (forceRebuild ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) == null)
                {
                    BuildTestScene();
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"Health Bar assets are ready in '{RootFolder}'.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void BuildPrefab()
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject healthBarObject = CreateHealthBarHierarchy();
                SceneManager.MoveGameObjectToScene(healthBarObject, previewScene);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    healthBarObject,
                    PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Could not create the health-bar prefab at '{PrefabPath}'.");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            AssetDatabase.ImportAsset(
                PrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static GameObject CreateHealthBarHierarchy()
        {
            var root = new GameObject(
                "Hand Drawn Health Bar",
                typeof(RectTransform),
                typeof(HealthBarController));
            root.layer = LayerMask.NameToLayer("UI");

            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchoredRect(
                rootRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(720f, 112f),
                Vector2.zero);

            Text label = CreateText("Label", root.transform, "VIDA");
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -1f);
            labelRect.sizeDelta = new Vector2(0f, 34f);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            label.color = GoldColor;
            label.gameObject
                .AddComponent<HealthBarHandDrawnOutlineEffect>()
                .Configure(2.5f, OutlineColor, 8f, 3, 71f);

            Image frame = CreateImage("Frame", root.transform, GoldColor);
            RectTransform frameRect = frame.rectTransform;
            frameRect.anchorMin = new Vector2(0f, 0f);
            frameRect.anchorMax = new Vector2(1f, 0f);
            frameRect.pivot = new Vector2(0.5f, 0f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.sizeDelta = new Vector2(0f, 66f);
            frame.gameObject
                .AddComponent<HealthBarHandDrawnOutlineEffect>()
                .Configure(6f, OutlineColor, 8f, 3, 43f);

            Image innerBackground = CreateImage(
                "Inner Background",
                frame.transform,
                DarkPanelColor);
            Stretch(innerBackground.rectTransform, 9f);
            innerBackground.gameObject.AddComponent<RectMask2D>();

            Image fill = CreateImage(
                "Fill",
                innerBackground.transform,
                new Color(0.20f, 0.57f, 0.39f, 1f));
            Stretch(fill.rectTransform, 3f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.gameObject
                .AddComponent<HealthBarHandDrawnOutlineEffect>()
                .Configure(2f, OutlineColor, 8f, 3, 127f);

            Image highlight = CreateImage(
                "Highlight",
                fill.transform,
                new Color(1f, 0.93f, 0.72f, 0.13f));
            RectTransform highlightRect = highlight.rectTransform;
            highlightRect.anchorMin = new Vector2(0f, 0.55f);
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;

            Text valueText = CreateText(
                "Health Value",
                innerBackground.transform,
                "100 / 100");
            Stretch(valueText.rectTransform, 0f);
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.fontSize = 25;
            valueText.fontStyle = FontStyle.Bold;
            valueText.color = new Color(0.96f, 0.91f, 0.78f, 1f);

            Shadow valueShadow = valueText.gameObject.AddComponent<Shadow>();
            valueShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            valueShadow.effectDistance = new Vector2(2f, -2f);

            HealthBarController controller = root.GetComponent<HealthBarController>();
            controller.ConfigureReferences(fill.rectTransform, fill, valueText);
            controller.SetHealth(100f, 100f);
            return root;
        }

        private static void BuildTestScene()
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene testScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(testScene);
                CreateTestCamera();

                Canvas canvas = CreateCanvas();
                CreateDemoBackground(canvas.transform);
                CreateDemoHeading(canvas.transform);

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"The health-bar prefab is missing at '{PrefabPath}'.");
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    prefab,
                    testScene);
                instance.name = "Health Bar Demo";
                instance.transform.SetParent(canvas.transform, false);

                RectTransform instanceRect = instance.GetComponent<RectTransform>();
                SetAnchoredRect(
                    instanceRect,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(720f, 112f),
                    new Vector2(0f, 35f));

                var demoObject = new GameObject(
                    "Automatic Health Bar Demo",
                    typeof(HealthBarDemoController));
                SceneManager.MoveGameObjectToScene(demoObject, testScene);
                demoObject
                    .GetComponent<HealthBarDemoController>()
                    .Configure(instance.GetComponent<HealthBarController>());

                Text instructions = CreateText(
                    "Instructions",
                    canvas.transform,
                    "DEMO AUTOMÁTICA  ·  100 → 0 → 100");
                SetAnchoredRect(
                    instructions.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(720f, 50f),
                    new Vector2(0f, -115f));
                instructions.alignment = TextAnchor.MiddleCenter;
                instructions.fontSize = 20;
                instructions.color = new Color(0.75f, 0.66f, 0.50f, 1f);

                if (!EditorSceneManager.SaveScene(testScene, TestScenePath))
                {
                    throw new InvalidOperationException(
                        $"Could not save the health-bar test scene at '{TestScenePath}'.");
                }
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);

                if (testScene.IsValid() && testScene.isLoaded)
                    EditorSceneManager.CloseScene(testScene, true);
            }
        }

        private static void CreateTestCamera()
        {
            var cameraObject = new GameObject("Health Bar Test Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DemoBackgroundColor;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject(
                "Health Bar Test Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateDemoBackground(Transform canvasTransform)
        {
            Image background = CreateImage(
                "Background",
                canvasTransform,
                DemoBackgroundColor);
            Stretch(background.rectTransform, 0f);
        }

        private static void CreateDemoHeading(Transform canvasTransform)
        {
            Text heading = CreateText(
                "Heading",
                canvasTransform,
                "BARRA DE VIDA");
            SetAnchoredRect(
                heading.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(900f, 90f),
                new Vector2(0f, 230f));
            heading.alignment = TextAnchor.MiddleCenter;
            heading.fontSize = 48;
            heading.fontStyle = FontStyle.Bold;
            heading.color = GoldColor;
            heading.gameObject
                .AddComponent<HealthBarHandDrawnOutlineEffect>()
                .Configure(4f, OutlineColor, 8f, 3, 311f);
        }

        private static Image CreateImage(
            string objectName,
            Transform parent,
            Color color)
        {
            var imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.layer = LayerMask.NameToLayer("UI");
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            string content)
        {
            var textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.layer = LayerMask.NameToLayer("UI");
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = ResolveFont();
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetAnchoredRect(
            RectTransform rectTransform,
            Vector2 anchor,
            Vector2 size,
            Vector2 position)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            rectTransform.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rectTransform, float inset)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(inset, inset);
            rectTransform.offsetMax = new Vector2(-inset, -inset);
            rectTransform.localScale = Vector3.one;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            int separatorIndex = folderPath.LastIndexOf('/');
            if (separatorIndex <= 0)
                throw new InvalidOperationException($"Invalid asset folder '{folderPath}'.");

            string parentPath = folderPath.Substring(0, separatorIndex);
            string folderName = folderPath.Substring(separatorIndex + 1);
            EnsureAssetFolder(parentPath);

            string folderGuid = AssetDatabase.CreateFolder(parentPath, folderName);
            if (string.IsNullOrEmpty(folderGuid))
                throw new InvalidOperationException($"Could not create '{folderPath}'.");
        }

        private static Font ResolveFont()
        {
            Font moduleFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            return moduleFont != null
                ? moduleFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
