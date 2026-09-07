using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class WinScreenController : MonoBehaviour
{
    [Header("Reveal")]
    [SerializeField]
    [Tooltip("Controls the opacity and interaction of the complete victory screen.")]
    private CanvasGroup screenGroup;

    [SerializeField, Min(0f)]
    [Tooltip("Seconds used to reveal the complete victory screen.")]
    private float fadeDuration = 1.1f;

    [Header("Scene Navigation")]
    [SerializeField]
    [Tooltip("Build Settings path loaded by the Play Again button.")]
    private string playAgainScenePath = "Assets/Scenes/SampleScene.unity";

    [SerializeField]
    [Tooltip("Build Settings path loaded by the Exit button.")]
    private string mainMenuScenePath = "Assets/Scenes/Main Menu.unity";

    private Coroutine revealRoutine;

    public bool IsRevealing => revealRoutine != null;

    private void Awake()
    {
        ResolveReferences();
        EnsureEventSystem();
        HideImmediately();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            ShowVictory();
    }

    private void OnDisable()
    {
        StopRevealRoutine();
    }

    private void OnValidate()
    {
        fadeDuration = Mathf.Max(0f, fadeDuration);
        ResolveReferences();
    }

    public void ShowVictory()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "Win Screen: the controller must be active to reveal the victory screen.",
                this);
            return;
        }

        ResolveReferences();
        StopRevealRoutine();
        SetScreenState(0f, false, false);
        revealRoutine = StartCoroutine(RevealScreen());
    }

    public void HideImmediately()
    {
        StopRevealRoutine();
        SetScreenState(0f, false, false);
    }

    public void PlayAgain()
    {
        LoadConfiguredScene(playAgainScenePath, "play again");
    }

    public void ExitToMainMenu()
    {
        LoadConfiguredScene(mainMenuScenePath, "main menu");
    }

    public void Configure(
        CanvasGroup completeScreenGroup,
        float revealDuration,
        string playAgainPath,
        string menuPath)
    {
        screenGroup = completeScreenGroup;
        fadeDuration = Mathf.Max(0f, revealDuration);
        playAgainScenePath = playAgainPath;
        mainMenuScenePath = menuPath;
        HideImmediately();
    }

    private IEnumerator RevealScreen()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = fadeDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeDuration)
                : 1f;
            SetScreenState(Mathf.SmoothStep(0f, 1f, progress), false, false);
            yield return null;
        }

        SetScreenState(1f, true, true);
        revealRoutine = null;
    }

    private void LoadConfiguredScene(string scenePath, string destinationName)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError(
                $"Win Screen: no {destinationName} scene path has been configured.",
                this);
            return;
        }

        string normalizedPath = scenePath.Trim();
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(normalizedPath);
        if (buildIndex < 0)
        {
            Debug.LogError(
                $"Win Screen: scene '{normalizedPath}' is not enabled in Build Settings.",
                this);
            return;
        }

        SceneManager.LoadScene(buildIndex);
    }

    private void StopRevealRoutine()
    {
        if (revealRoutine == null)
            return;

        StopCoroutine(revealRoutine);
        revealRoutine = null;
    }

    private void ResolveReferences()
    {
        if (screenGroup == null)
            screenGroup = GetComponent<CanvasGroup>();
    }

    private void SetScreenState(float alpha, bool interactable, bool blocksRaycasts)
    {
        if (screenGroup == null)
            return;

        screenGroup.alpha = alpha;
        screenGroup.interactable = interactable;
        screenGroup.blocksRaycasts = blocksRaycasts;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null ||
            UnityEngine.Object.FindAnyObjectByType<EventSystem>(
                FindObjectsInactive.Include) != null)
        {
            return;
        }

        var eventSystemObject = new GameObject(
            "Event System",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject
            .GetComponent<InputSystemUIInputModule>()
            .AssignDefaultActions();
    }
}
