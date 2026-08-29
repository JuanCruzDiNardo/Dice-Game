using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField] private List<WaveData> waves = new List<WaveData>();

    [Header("Spawners")]
    [SerializeField] private List<EnemySpawner> spawners = new List<EnemySpawner>();

    [Header("Upgrades")]
    [SerializeField] private DiceUpgradeManager diceUpgradeManager;

    [Header("Configuration")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private float timeBetweenWaves = 3f;

    [Header("Debug")]
    [SerializeField] private int currentWaveIndex = -1;
    [SerializeField] private int spawnedEnemies;
    [SerializeField] private int aliveEnemies;
    [SerializeField] private bool waveRunning;
    [SerializeField] private bool spawningFinished;
    [SerializeField] private bool waitingForUpgrade;

    private Coroutine waveRoutine;

    private readonly List<EnemyData> spawnQueue = new List<EnemyData>();
    private readonly HashSet<Enemy> aliveEnemiesList = new HashSet<Enemy>();
    private readonly List<Enemy> spawnedEnemiesList = new List<Enemy>();

    public int CurrentWaveIndex => currentWaveIndex;
    public int SpawnedEnemies => spawnedEnemies;
    public int AliveEnemies => aliveEnemies;
    public bool WaveRunning => waveRunning;
    public bool SpawningFinished => spawningFinished;
    public bool WaitingForUpgrade => waitingForUpgrade;
    public int TotalWaves => waves.Count;
    public WaveData CurrentWave => currentWaveIndex >= 0 && currentWaveIndex < waves.Count ? waves[currentWaveIndex] : null;

    public event Action<int, WaveData> OnWaveStarted;
    public event Action<int, WaveData> OnWaveFinished;
    public event Action<int, int> OnEnemyCountChanged;
    public event Action OnAllWavesFinished;

    private void Start()
    {
        if (diceUpgradeManager == null)
            diceUpgradeManager = DiceUpgradeManager.Instance;

        if (diceUpgradeManager != null)
            diceUpgradeManager.OnSelectionCompleted += HandleUpgradeSelectionCompleted;

        if (startAutomatically)
            StartNextWave();
    }

    private void OnDestroy()
    {
        if (diceUpgradeManager != null)
            diceUpgradeManager.OnSelectionCompleted -= HandleUpgradeSelectionCompleted;
    }

    // =========================================================
    // WAVE CONTROL
    // =========================================================

    public void StartNextWave()
    {
        StopWaveRoutine();
        ClearTrackedEnemies();
        ClearSpawnedEnemies();

        waitingForUpgrade = false;

        int nextWaveIndex = currentWaveIndex + 1;

        if (nextWaveIndex >= waves.Count)
        {
            currentWaveIndex = waves.Count;
            waveRunning = false;
            OnAllWavesFinished?.Invoke();
            return;
        }

        StartWave(nextWaveIndex);
    }

    public void StartWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogWarning($"Wave index {waveIndex} is invalid.");
            return;
        }

        WaveData wave = waves[waveIndex];

        if (wave == null)
        {
            Debug.LogWarning($"Wave {waveIndex} has no WaveData assigned.");
            return;
        }

        if (spawners.Count == 0)
        {
            Debug.LogWarning("WaveManager has no EnemySpawners assigned.");
            return;
        }

        StopWaveRoutine();
        ClearTrackedEnemies();
        ClearSpawnedEnemies();

        waitingForUpgrade = false;
        currentWaveIndex = waveIndex;
        waveRoutine = StartCoroutine(WaveRoutine(wave));
    }

    public void StopCurrentWave()
    {
        StopWaveRoutine();
        ClearTrackedEnemies();

        waveRunning = false;
        spawningFinished = false;
        waitingForUpgrade = false;
    }

    public void SkipCurrentWave()
    {
        StopWaveRoutine();
        ClearTrackedEnemies();
        ClearSpawnedEnemies();

        waveRunning = false;
        spawningFinished = false;
        waitingForUpgrade = false;

        StartNextWave();
    }

    public void RestartCurrentWave()
    {
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Count)
            return;

        StartWave(currentWaveIndex);
    }

    // =========================================================
    // WAVE ROUTINE
    // =========================================================

    private IEnumerator WaveRoutine(WaveData wave)
    {
        waveRunning = true;
        spawningFinished = false;
        spawnedEnemies = 0;
        aliveEnemies = 0;

        BuildSpawnQueue(wave);

        OnWaveStarted?.Invoke(currentWaveIndex, wave);
        OnEnemyCountChanged?.Invoke(aliveEnemies, spawnQueue.Count);

        if (spawnQueue.Count == 0)
        {
            spawningFinished = true;
            FinishCurrentWave();
            yield break;
        }

        float spawnInterval = spawnQueue.Count > 1 ? wave.duration / (spawnQueue.Count - 1) : 0f;

        for (int i = 0; i < spawnQueue.Count; i++)
        {
            SpawnEnemy(spawnQueue[i]);

            if (i < spawnQueue.Count - 1 && spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }

        spawningFinished = true;

        while (aliveEnemies > 0)
            yield return null;

        FinishCurrentWave();
    }

    // =========================================================
    // SPAWN
    // =========================================================

    private void BuildSpawnQueue(WaveData wave)
    {
        spawnQueue.Clear();

        foreach (WaveData.EnemyGroup group in wave.enemies)
        {
            if (group == null)
                continue;

            if (group.enemyData == null)
                continue;

            for (int i = 0; i < group.amount; i++)
                spawnQueue.Add(group.enemyData);
        }

        ShuffleSpawnQueue();
    }

    private void ShuffleSpawnQueue()
    {
        for (int i = spawnQueue.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            EnemyData temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[randomIndex];
            spawnQueue[randomIndex] = temp;
        }
    }

    private void SpawnEnemy(EnemyData enemyData)
    {
        EnemySpawner spawner = GetRandomSpawner();

        if (spawner == null)
            return;

        Enemy enemy = spawner.SpawnEnemy(enemyData);

        if (enemy == null)
            return;

        spawnedEnemiesList.Add(enemy);
        aliveEnemiesList.Add(enemy);

        enemy.OnDeath += HandleEnemyDeath;

        spawnedEnemies++;
        aliveEnemies++;

        OnEnemyCountChanged?.Invoke(aliveEnemies, spawnQueue.Count);
    }

    private EnemySpawner GetRandomSpawner()
    {
        if (spawners.Count == 0)
            return null;

        return spawners[UnityEngine.Random.Range(0, spawners.Count)];
    }

    // =========================================================
    // ENEMY TRACKING
    // =========================================================

    private void HandleEnemyDeath(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!aliveEnemiesList.Remove(enemy))
            return;

        enemy.OnDeath -= HandleEnemyDeath;

        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);

        OnEnemyCountChanged?.Invoke(aliveEnemies, spawnQueue.Count);
    }

    private void ClearTrackedEnemies()
    {
        foreach (Enemy enemy in aliveEnemiesList)
        {
            if (enemy != null)
                enemy.OnDeath -= HandleEnemyDeath;
        }

        aliveEnemiesList.Clear();
        aliveEnemies = 0;
    }

    private void ClearSpawnedEnemies()
    {
        foreach (Enemy enemy in spawnedEnemiesList)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        spawnedEnemiesList.Clear();
    }

    // =========================================================
    // FINISH
    // =========================================================

    private void FinishCurrentWave()
    {
        waveRunning = false;
        waveRoutine = null;

        WaveData finishedWave = CurrentWave;

        OnWaveFinished?.Invoke(currentWaveIndex, finishedWave);

        ClearTrackedEnemies();

        if (currentWaveIndex >= waves.Count - 1)
        {
            waitingForUpgrade = false;
            OnAllWavesFinished?.Invoke();
            return;
        }

        StartUpgradeSelection();
    }

    private void StartUpgradeSelection()
    {
        if (diceUpgradeManager == null)
        {
            StartNextWaveDelay();
            return;
        }

        bool optionsGenerated = diceUpgradeManager.GenerateOptions();

        if (!optionsGenerated)
        {
            StartNextWaveDelay();
            return;
        }

        waitingForUpgrade = true;
    }

    private void HandleUpgradeSelectionCompleted()
    {
        if (!waitingForUpgrade)
            return;

        waitingForUpgrade = false;

        StartNextWaveDelay();
    }

    private void StartNextWaveDelay()
    {
        StopWaveRoutine();
        waveRoutine = StartCoroutine(NextWaveDelayRoutine());
    }

    private IEnumerator NextWaveDelayRoutine()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        waveRoutine = null;

        StartNextWave();
    }

    private void StopWaveRoutine()
    {
        if (waveRoutine == null)
            return;

        StopCoroutine(waveRoutine);
        waveRoutine = null;
    }
}