using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    private enum MenuSection
    {
        Main,
        Options
    }

    [Header("Panels")]
    [SerializeField]
    private GameObject mainActionsPanel;

    [SerializeField]
    private GameObject optionsPanel;

    [Header("Default Selection")]
    [SerializeField]
    private Selectable mainDefaultSelection;

    [SerializeField]
    private Selectable optionsDefaultSelection;

    [Header("Gameplay")]
    [SerializeField]
    [Tooltip("Scene path registered in Build Settings. Change this when the final gameplay scene is ready.")]
    private string gameplayScenePath = "Assets/Scenes/SampleScene.unity";

    private MenuSection activeSection;

    private void Awake()
    {
        ShowMainMenu();
    }

    public void Configure(
        GameObject mainPanel,
        GameObject optionsPanelReference,
        Selectable mainSelection,
        Selectable optionsSelection,
        string gameplayPath)
    {
        mainActionsPanel = mainPanel;
        optionsPanel = optionsPanelReference;
        mainDefaultSelection = mainSelection;
        optionsDefaultSelection = optionsSelection;
        gameplayScenePath = gameplayPath;
    }

    public void PlayGame()
    {
        if (string.IsNullOrWhiteSpace(gameplayScenePath))
        {
            Debug.LogError("Main Menu: no gameplay scene has been configured.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplayScenePath))
        {
            Debug.LogError(
                $"Main Menu: gameplay scene '{gameplayScenePath}' is not available in Build Settings.",
                this);
            return;
        }

        SceneManager.LoadScene(gameplayScenePath);
    }

    public void ShowOptions()
    {
        SetSection(MenuSection.Options);
    }

    public void ShowMainMenu()
    {
        SetSection(MenuSection.Main);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetSection(MenuSection section)
    {
        activeSection = section;

        if (mainActionsPanel != null)
            mainActionsPanel.SetActive(activeSection == MenuSection.Main);

        if (optionsPanel != null)
            optionsPanel.SetActive(activeSection == MenuSection.Options);

        Selectable targetSelection = activeSection == MenuSection.Main
            ? mainDefaultSelection
            : optionsDefaultSelection;

        if (targetSelection != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(targetSelection.gameObject);
    }
}
