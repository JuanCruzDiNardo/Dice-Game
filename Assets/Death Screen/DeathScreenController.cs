using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class DeathScreenController : MonoBehaviour
{
    [Header("Sequence Elements")]
    [SerializeField]
    [Tooltip("Independent black layer rendered behind all death-screen artwork.")]
    private CanvasGroup blackOverlay;

    [SerializeField]
    private CanvasGroup retryButtonGroup;

    [SerializeField]
    private CanvasGroup exitButtonGroup;

    [SerializeField]
    private CanvasGroup titleGroup;

    [SerializeField]
    [Tooltip("Background, shadow, dead mage and light. These elements appear instantly together.")]
    private CanvasGroup[] finalArtworkGroups = Array.Empty<CanvasGroup>();

    [Header("Timing")]
    [SerializeField]
    private DeathScreenSequenceSettings sequence = new();

    [Header("Scene Navigation")]
    [SerializeField]
    [Tooltip("Build Settings path of the scene that Retry should load.")]
    private string retryScenePath = "Assets/Scenes/SampleScene.unity";

    [SerializeField]
    [Tooltip("Build Settings path of the main menu scene.")]
    private string mainMenuScenePath = "Assets/Scenes/Main Menu.unity";

    private Coroutine revealRoutine;

    public bool IsSequencePlaying => revealRoutine != null;

    private void Awake()
    {
        sequence ??= new DeathScreenSequenceSettings();
        sequence.Validate();
        EnsureEventSystem();
        HideImmediately();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            ShowGameOver();
    }

    private void OnDisable()
    {
        StopRevealRoutine();
    }

    private void OnValidate()
    {
        sequence ??= new DeathScreenSequenceSettings();
        sequence.Validate();
    }

    public void ShowGameOver()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("Death Screen: the controller must be active to play its sequence.", this);
            return;
        }

        StopRevealRoutine();
        PrepareSequenceStart();
        revealRoutine = StartCoroutine(RevealSequence());
    }

    public void HideImmediately()
    {
        StopRevealRoutine();
        SetGroupState(blackOverlay, 0f, false, false);
        SetGroupState(retryButtonGroup, 0f, false, false);
        SetGroupState(exitButtonGroup, 0f, false, false);
        SetGroupState(titleGroup, 0f, false, false);
        SetArtworkAlpha(0f);
    }

    public void Retry()
    {
        LoadConfiguredScene(retryScenePath, "retry");
    }

    public void ExitToMainMenu()
    {
        LoadConfiguredScene(mainMenuScenePath, "main menu");
    }

    public void Configure(
        CanvasGroup blackLayer,
        CanvasGroup retryGroup,
        CanvasGroup exitGroup,
        CanvasGroup defeatTitleGroup,
        CanvasGroup[] artworkGroups,
        string retryPath,
        string menuPath)
    {
        blackOverlay = blackLayer;
        retryButtonGroup = retryGroup;
        exitButtonGroup = exitGroup;
        titleGroup = defeatTitleGroup;
        finalArtworkGroups = artworkGroups ?? Array.Empty<CanvasGroup>();
        retryScenePath = retryPath;
        mainMenuScenePath = menuPath;

        sequence ??= new DeathScreenSequenceSettings();
        sequence.Validate();
        HideImmediately();
    }

    private IEnumerator RevealSequence()
    {
        yield return FadeGroup(
            blackOverlay,
            sequence.BlackFadeDuration,
            makeInteractable: false,
            blocksRaycastsAfterFade: true);

        yield return WaitUnscaled(sequence.BlackScreenPause);

        yield return FadeGroup(
            retryButtonGroup,
            sequence.ButtonFadeDuration,
            makeInteractable: true,
            blocksRaycastsAfterFade: true);

        yield return WaitUnscaled(sequence.PauseBetweenButtons);

        yield return FadeGroup(
            exitButtonGroup,
            sequence.ButtonFadeDuration,
            makeInteractable: true,
            blocksRaycastsAfterFade: true);

        yield return WaitUnscaled(sequence.PauseBeforeTitle);

        yield return FadeGroup(
            titleGroup,
            sequence.TitleFadeDuration,
            makeInteractable: false,
            blocksRaycastsAfterFade: false);

        yield return WaitUnscaled(sequence.PauseBeforeFinalArtwork);
        SetArtworkAlpha(1f);

        revealRoutine = null;
    }

    private void PrepareSequenceStart()
    {
        SetGroupState(blackOverlay, 0f, false, true);
        SetGroupState(retryButtonGroup, 0f, false, false);
        SetGroupState(exitButtonGroup, 0f, false, false);
        SetGroupState(titleGroup, 0f, false, false);
        SetArtworkAlpha(0f);
    }

    private IEnumerator FadeGroup(
        CanvasGroup group,
        float duration,
        bool makeInteractable,
        bool blocksRaycastsAfterFade)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        group.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            group.alpha = Mathf.SmoothStep(0f, 1f, progress);
            yield return null;
        }

        SetGroupState(
            group,
            1f,
            makeInteractable,
            blocksRaycastsAfterFade);
    }

    private static IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetArtworkAlpha(float alpha)
    {
        if (finalArtworkGroups == null)
            return;

        for (int index = 0; index < finalArtworkGroups.Length; index++)
            SetGroupState(finalArtworkGroups[index], alpha, false, false);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var eventSystemObject = new GameObject(
            "Event System",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject
            .GetComponent<InputSystemUIInputModule>()
            .AssignDefaultActions();
    }

    private void LoadConfiguredScene(string scenePath, string destinationName)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError(
                $"Death Screen: no {destinationName} scene path has been configured.",
                this);
            return;
        }

        string normalizedPath = scenePath.Trim();
        int sceneBuildIndex = SceneUtility.GetBuildIndexByScenePath(normalizedPath);
        if (sceneBuildIndex < 0)
        {
            Debug.LogError(
                $"Death Screen: scene '{normalizedPath}' is not enabled in Build Settings.",
                this);
            return;
        }

        SceneManager.LoadScene(sceneBuildIndex);
    }

    private void StopRevealRoutine()
    {
        if (revealRoutine == null)
            return;

        StopCoroutine(revealRoutine);
        revealRoutine = null;
    }

    private static void SetGroupState(
        CanvasGroup group,
        float alpha,
        bool interactable,
        bool blocksRaycasts)
    {
        if (group == null)
            return;

        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = blocksRaycasts;
    }
}
