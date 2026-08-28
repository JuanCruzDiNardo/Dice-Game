using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Paused,
        Victory,
        Defeat
    }

    [Header("References")]
    [SerializeField] private Nexus nexus;
    [SerializeField] private WaveManager waveManager;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (nexus != null)
            nexus.OnDestroyed += HandleNexusDestroyed;

        if (waveManager != null)
            waveManager.OnAllWavesFinished += HandleAllWavesFinished;
    }

    private void OnDisable()
    {
        if (nexus != null)
            nexus.OnDestroyed -= HandleNexusDestroyed;

        if (waveManager != null)
            waveManager.OnAllWavesFinished -= HandleAllWavesFinished;
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        HandlePauseInput();
    }

    private void InitializeGame()
    {
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (defeatPanel != null)
            defeatPanel.SetActive(false);

        ChangeState(GameState.Playing);
    }

    private void HandlePauseInput()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        TogglePause();
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            PauseGame();
            return;
        }

        if (CurrentState == GameState.Paused)
            ResumeGame();
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing)
            return;

        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused)
            return;

        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        ChangeState(GameState.Playing);
    }

    private void HandleNexusDestroyed()
    {
        if (CurrentState != GameState.Playing)
            return;

        Defeat();
    }

    private void HandleAllWavesFinished()
    {
        if (CurrentState != GameState.Playing)
            return;

        Victory();
    }

    private void Victory()
    {
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        ChangeState(GameState.Victory);

        Debug.Log("VICTORIA");
    }

    private void Defeat()
    {
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (defeatPanel != null)
            defeatPanel.SetActive(true);

        ChangeState(GameState.Defeat);

        Debug.Log("DERROTA");
    }

    private void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("No hay una escena de menú principal configurada.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogWarning($"La escena '{mainMenuSceneName}' no existe o no está agregada al Build Profile.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public bool IsGamePlaying()
    {
        return CurrentState == GameState.Playing;
    }

    public bool IsGamePaused()
    {
        return CurrentState == GameState.Paused;
    }

    public bool IsGameOver()
    {
        return CurrentState == GameState.Victory || CurrentState == GameState.Defeat;
    }
}