using TMPro;
using UnityEngine;

public class WaveCounterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameManager gameManager;

    [Header("Wave Label")]
    [SerializeField] private TMP_Text waveLabel;
    [SerializeField] private string wavePrefix = "WAVE ";

    [Header("Defeat Label")]
    [SerializeField] private TMP_Text defeatWaveLabel;
    [SerializeField] private string defeatPrefix = "WAVE ALCANZADA: ";

    private void OnEnable()
    {
        if (waveManager != null)
            waveManager.OnWaveNumberChanged += UpdateWaveLabel;

        if (gameManager != null)
            gameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void Start()
    {
        if (waveManager != null && waveManager.CurrentWaveNumber > 0)
            UpdateWaveLabel(waveManager.CurrentWaveNumber);
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.OnWaveNumberChanged -= UpdateWaveLabel;

        if (gameManager != null)
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void UpdateWaveLabel(int waveNumber)
    {
        if (waveLabel != null)
            waveLabel.text = $"{wavePrefix}{waveNumber}";
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        if (newState != GameManager.GameState.Defeat)
            return;

        UpdateDefeatWaveLabel();
    }

    private void UpdateDefeatWaveLabel()
    {
        if (defeatWaveLabel == null || waveManager == null)
            return;

        defeatWaveLabel.text = $"{defeatPrefix}{waveManager.CurrentWaveNumber}";
    }
}