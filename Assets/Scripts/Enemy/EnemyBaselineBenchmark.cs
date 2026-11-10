#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EnemyBaselineBenchmark : MonoBehaviour
{
    private const string MainScene = "Assets/Scenes/Main Scene.unity";
    private const string PendingCount = "DiceGame.EnemyBaselineBenchmark.PendingCount";
    private const int Seed = 12345;
    private const float WarmupSeconds = 2f;
    private const float SampleSeconds = 10f;
    private static readonly ProfilerMarker SamplingMarker = new("DiceGame.Benchmark.Sampling");

    private readonly List<float> frameTimes = new(4096);
    private Enemy[] enemies;
    private double warmupStart = -1;
    private double sampledSeconds;
    private bool measuring;
    private int previousVSync;
    private int previousTargetFrameRate;
    private bool changedFrameSettings;

    [MenuItem("Dice Game/Rendimiento/Medir 100 enemigos")]
    private static void Measure100() => EnterBenchmark(100);

    [MenuItem("Dice Game/Rendimiento/Medir 500 enemigos")]
    private static void Measure500() => EnterBenchmark(500);

    [MenuItem("Dice Game/Rendimiento/Medir 1000 enemigos")]
    private static void Measure1000() => EnterBenchmark(1000);

    private static void EnterBenchmark(int count)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Benchmark: salí de Play antes de iniciar otra medición.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        SceneAsset startScene = EditorSceneManager.playModeStartScene;
        if (scene.path != MainScene || SceneManager.sceneCount != 1 || scene.isDirty ||
            (startScene != null && AssetDatabase.GetAssetPath(startScene) != MainScene))
        {
            Debug.LogWarning("Benchmark: abrí únicamente Main Scene, guardá la escena y verificá la escena de inicio de Play.");
            return;
        }

        SessionState.SetInt(PendingCount, count);
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartRequestedRun()
    {
        int count = SessionState.GetInt(PendingCount, 0);
        SessionState.EraseInt(PendingCount);
        if (count == 0 || SceneManager.GetActiveScene().path != MainScene)
            return;

        WaveManager waves = FindAnyObjectByType<WaveManager>();
        GameObject floor = GameObject.Find("TrayFloor");
        BoxCollider tray = floor != null ? floor.GetComponent<BoxCollider>() : null;
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Orco.prefab");
        Enemy enemyPrefab = prefab != null ? prefab.GetComponent<Enemy>() : null;
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Scripts/Enemy/Enemy 1.asset");
        if (waves == null || tray == null || spawner == null || enemyPrefab == null || data == null || Nexus.Instance == null)
        {
            Debug.LogError("Benchmark: faltan referencias del escenario.");
            EditorApplication.ExitPlaymode();
            return;
        }

        waves.enabled = false;
        var runner = new GameObject("Enemy Baseline Benchmark").AddComponent<EnemyBaselineBenchmark>();
        runner.Initialize(count, enemyPrefab, data, tray.bounds, spawner.transform.position.y);
    }

    private void Initialize(int count, Enemy prefab, EnemyData data, Bounds bounds, float y)
    {
        previousVSync = QualitySettings.vSyncCount;
        previousTargetFrameRate = Application.targetFrameRate;
        changedFrameSettings = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        enemies = new Enemy[count];
        var layout = new System.Random(Seed);
        UnityEngine.Random.InitState(Seed);
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count * bounds.size.x / bounds.size.z));
        int rows = Mathf.CeilToInt((float)count / columns);
        for (int i = 0; i < count; i++)
        {
            float u = (i % columns + 0.25f + 0.5f * (float)layout.NextDouble()) / columns;
            float v = (i / columns + 0.25f + 0.5f * (float)layout.NextDouble()) / rows;
            float x = Mathf.Lerp(bounds.min.x + 0.5f, bounds.max.x - 0.5f, u);
            float z = Mathf.Lerp(bounds.min.z + 0.5f, bounds.max.z - 0.5f, v);
            Enemy enemy = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity);
            enemy.Initialize(data);
            enemy.KeepAliveForBenchmark = true;
            enemies[i] = enemy;
        }

        Debug.Log($"Benchmark: {count} enemigos, semilla {Seed}. Calentamiento de {WarmupSeconds} s y medición de {SampleSeconds} s.");
    }

    private void LateUpdate()
    {
        float frameSeconds = Time.unscaledDeltaTime;
        if (!measuring)
        {
            double now = Time.unscaledTimeAsDouble;
            if (warmupStart < 0)
                warmupStart = now;
            if (now - warmupStart >= WarmupSeconds)
                measuring = true;
            return;
        }

        using (SamplingMarker.Auto())
        {
            frameTimes.Add(frameSeconds * 1000f);
            sampledSeconds += frameSeconds;
        }

        if (sampledSeconds < SampleSeconds)
            return;

        enabled = false;
        int missing = 0;
        int dead = 0;
        int inactive = 0;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                missing++;
            else if (enemy.IsDead)
                dead++;
            else if (!enemy.isActiveAndEnabled)
                inactive++;
        }

        if (missing + dead + inactive > 0)
        {
            Debug.LogError($"Benchmark descartado | esperados: {enemies.Length} | ausentes: {missing} | muertos: {dead} | inactivos: {inactive}.");
            return;
        }

        SaveResults();
    }

    private void SaveResults()
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Benchmarks"));
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string path = Path.Combine(directory, $"baseline-{enemies.Length}-{stamp}.csv");
        Directory.CreateDirectory(directory);
        using (var writer = new StreamWriter(path))
        {
            writer.WriteLine("enemies,seed,frame,frame_ms");
            for (int i = 0; i < frameTimes.Count; i++)
                writer.WriteLine(FormattableString.Invariant($"{enemies.Length},{Seed},{i + 1},{frameTimes[i]:F4}"));
        }

        frameTimes.Sort();
        float median = (frameTimes[(frameTimes.Count - 1) / 2] + frameTimes[frameTimes.Count / 2]) * 0.5f;
        float p95 = frameTimes[Mathf.CeilToInt(frameTimes.Count * 0.95f) - 1];
        double mean = sampledSeconds * 1000.0 / frameTimes.Count;
        Debug.Log(FormattableString.Invariant(
            $"Benchmark terminado | {enemies.Length} enemigos | {frameTimes.Count} cuadros | media {mean:F2} ms | mediana {median:F2} ms | p95 {p95:F2} ms\nCSV: {path}"));
    }

    private void OnDestroy()
    {
        if (!changedFrameSettings)
            return;

        QualitySettings.vSyncCount = previousVSync;
        Application.targetFrameRate = previousTargetFrameRate;
    }
}
#endif
