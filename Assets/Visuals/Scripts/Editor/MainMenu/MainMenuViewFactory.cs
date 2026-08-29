using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public readonly struct MainMenuViewReferences
{
    public MainMenuViewReferences(
        GameObject mainActionsPanel,
        GameObject optionsPanel,
        Button playButton,
        Button optionsButton,
        Button backButton,
        Button exitButton)
    {
        MainActionsPanel = mainActionsPanel;
        OptionsPanel = optionsPanel;
        PlayButton = playButton;
        OptionsButton = optionsButton;
        BackButton = backButton;
        ExitButton = exitButton;
    }

    public GameObject MainActionsPanel { get; }
    public GameObject OptionsPanel { get; }
    public Button PlayButton { get; }
    public Button OptionsButton { get; }
    public Button BackButton { get; }
    public Button ExitButton { get; }
}

public sealed class MainMenuViewFactory
{
    private static readonly Color InkColor = new(0.035f, 0.03f, 0.025f, 1f);
    private static readonly Color ParchmentButtonColor = new(0.82f, 0.67f, 0.39f, 0.98f);
    private static readonly Color HoverButtonColor = new(0.96f, 0.78f, 0.31f, 1f);
    private static readonly Color PressedButtonColor = new(0.20f, 0.57f, 0.39f, 1f);

    private readonly TMP_FontAsset font;
    private readonly Texture paperTexture;
    private readonly Sprite wizardSprite;
    private readonly Sprite diceSprite;

    public MainMenuViewFactory(
        TMP_FontAsset menuFont,
        Texture backgroundTexture,
        Sprite wizardArtwork,
        Sprite diceArtwork)
    {
        font = menuFont;
        paperTexture = backgroundTexture;
        wizardSprite = wizardArtwork;
        diceSprite = diceArtwork;
    }

    public MainMenuViewReferences Build(Transform sceneRoot)
    {
        Camera uiCamera = CreateCamera(sceneRoot);
        Canvas canvas = CreateCanvas(sceneRoot, uiCamera);
        CreateBackground(canvas.transform);
        CreateInkWash(canvas.transform);

        GameObject mainPanel = CreatePanel(canvas.transform, "Main Actions", new Vector2(470f, 390f));
        Button playButton = CreateMenuButton(mainPanel.transform, "Jugar", new Vector2(0f, 128f), new Vector2(430f, 102f), 54f);
        Button optionsButton = CreateMenuButton(mainPanel.transform, "Opciones", Vector2.zero, new Vector2(430f, 102f), 54f);
        Button exitButton = CreateMenuButton(mainPanel.transform, "Salir", new Vector2(0f, -128f), new Vector2(430f, 102f), 54f);

        GameObject optionsPanel = CreatePanel(canvas.transform, "Options", new Vector2(470f, 470f));
        TextMeshProUGUI heading = CreateText(optionsPanel.transform, "Options Heading", "Opciones", 62f);
        SetCenteredRect(heading.rectTransform, new Vector2(430f, 92f), new Vector2(0f, 160f));
        heading.alignment = TextAlignmentOptions.MidlineLeft;

        Button backButton = CreateMenuButton(
            optionsPanel.transform,
            "Volver",
            new Vector2(-80f, -178f),
            new Vector2(270f, 72f),
            40f);

        CreateCharacterArtwork(canvas.transform);
        CreateEventSystem(sceneRoot);

        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);

        return new MainMenuViewReferences(
            mainPanel,
            optionsPanel,
            playButton,
            optionsButton,
            backButton,
            exitButton);
    }

    private static Camera CreateCamera(Transform parent)
    {
        var cameraObject = new GameObject("UI Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(parent, false);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.09f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        return camera;
    }

    private static Canvas CreateCanvas(Transform parent, Camera uiCamera)
    {
        GameObject canvasObject = CreateUiObject("Main Menu Canvas", parent);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = 1f;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void CreateBackground(Transform parent)
    {
        GameObject backgroundObject = CreateUiObject("Paper Background", parent);
        Stretch(backgroundObject.GetComponent<RectTransform>());

        RawImage background = backgroundObject.AddComponent<RawImage>();
        background.texture = paperTexture;
        background.color = new Color(0.84f, 0.78f, 0.64f, 1f);
        background.raycastTarget = false;
    }

    private static void CreateInkWash(Transform parent)
    {
        GameObject washObject = CreateUiObject("Ink Wash", parent);
        Stretch(washObject.GetComponent<RectTransform>());

        Image wash = washObject.AddComponent<Image>();
        wash.color = new Color(0.06f, 0.13f, 0.09f, 0.20f);
        wash.raycastTarget = false;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panelObject = CreateUiObject(name, parent);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.17f, 0.5f);
        panelRect.anchorMax = new Vector2(0.17f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = size;
        return panelObject;
    }

    private Button CreateMenuButton(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        GameObject buttonObject = CreateUiObject($"{label} Button", parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(buttonRect, size, position);

        Image background = buttonObject.AddComponent<Image>();
        background.color = ParchmentButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = ParchmentButtonColor,
            highlightedColor = HoverButtonColor,
            pressedColor = PressedButtonColor,
            selectedColor = HoverButtonColor,
            disabledColor = new Color(0.45f, 0.42f, 0.36f, 0.75f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        HandDrawnOutlineEffect outline = buttonObject.AddComponent<HandDrawnOutlineEffect>();
        outline.Configure(6f, InkColor, 8f, 3, GetStableSeed(label));
        buttonObject.AddComponent<HandDrawnButtonFeedback>();

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, fontSize);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(42f, 0f, 20f, 0f);
        Stretch(text.rectTransform);
        return button;
    }

    private void CreateCharacterArtwork(Transform parent)
    {
        GameObject artworkObject = CreateUiObject("Character Artwork", parent);
        RectTransform artworkRect = artworkObject.GetComponent<RectTransform>();
        artworkRect.anchorMin = new Vector2(0.72f, 0.5f);
        artworkRect.anchorMax = new Vector2(0.72f, 0.5f);
        artworkRect.pivot = new Vector2(0.5f, 0.5f);
        artworkRect.sizeDelta = new Vector2(790f, 790f);

        Image wizard = CreateArtworkImage(artworkObject.transform, "Mago", wizardSprite, 4.5f, 43f);
        Image dice = CreateArtworkImage(artworkObject.transform, "Dados", diceSprite, 4f, 71f);
        Stretch(wizard.rectTransform);
        Stretch(dice.rectTransform);

        FloatingUIElement floatingDice = dice.gameObject.AddComponent<FloatingUIElement>();
        floatingDice.Configure(13f, 0.52f, 0.25f);
    }

    private static Image CreateArtworkImage(
        Transform parent,
        string name,
        Sprite sprite,
        float outlineThickness,
        float seed)
    {
        GameObject imageObject = CreateUiObject(name, parent);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        HandDrawnOutlineEffect outline = imageObject.AddComponent<HandDrawnOutlineEffect>();
        outline.Configure(outlineThickness, InkColor, 8f, 3, seed);
        return image;
    }

    private TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string content,
        float fontSize)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.color = InkColor;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void CreateEventSystem(Transform parent)
    {
        var eventSystemObject = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetParent(parent, false);
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetCenteredRect(RectTransform rectTransform, Vector2 size, Vector2 position)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;
    }

    private static float GetStableSeed(string value)
    {
        int hash = 17;
        for (int index = 0; index < value.Length; index++)
            hash = hash * 31 + value[index];

        return hash;
    }
}
