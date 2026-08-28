using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Victory,
        Defeat,
        Paused
    }

    [Header("References")]
    [SerializeField] private Nexus nexus;
    [SerializeField] private WaveManager waveManager;

    [Header("UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

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

    private void Start()
    {
        InitializeGame();
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

    private void InitializeGame()
    {
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);

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
        ChangeState(GameState.Victory);
        victoryPanel.SetActive(true);

        Debug.Log("VICTORIA");
    }

    private void Defeat()
    {
        ChangeState(GameState.Defeat);
        defeatPanel.SetActive(true);

        Debug.Log("DERROTA");
    }

    private void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public bool IsGamePlaying()
    {
        return CurrentState == GameState.Playing;
    }

    public bool IsGameOver()
    {
        return CurrentState == GameState.Victory || CurrentState == GameState.Defeat;
    }
}