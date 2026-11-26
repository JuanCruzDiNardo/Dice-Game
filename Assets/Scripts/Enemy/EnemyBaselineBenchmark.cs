#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using Unity.Burst;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EnemyBaselineBenchmark : MonoBehaviour
{
    private const string MainScene = "Assets/Scenes/Main Scene.unity";
    private const string PendingCount = "DiceGame.EnemyBaselineBenchmark.PendingCount";
    private const string PendingMethod = "DiceGame.Benchmark.Method";
    private const string Suite = "DiceGame.Benchmark.Suite";
    private const string SuiteIndex = "DiceGame.Benchmark.SuiteIndex";
    private const string SuiteSucceeded = "DiceGame.Benchmark.SuiteSucceeded";
    private const string SuiteQuit = "DiceGame.Benchmark.SuiteQuit";
    private const string MenuRoot = "Dice Game/Rendimiento/";
    private static readonly int[] Loads = { 100, 500, 1000 };
    private static readonly EnemyBenchmarkMethod[] Methods =
    {
        EnemyBenchmarkMethod.Original, EnemyBenchmarkMethod.Sequential,
        EnemyBenchmarkMethod.Jobs, EnemyBenchmarkMethod.JobsBurst, EnemyBenchmarkMethod.Ecs
    };
    private const int Seed = 12345;
    private const float WarmupSeconds = 2f;
    private const float SampleSeconds = 10f;
    private static readonly ProfilerMarker SamplingMarker = new("DiceGame.Benchmark.Sampling");

    private readonly List<float> frameTimes = new(4096);
    private Enemy[] enemies;
    private EnemyBenchmarkMethod method;
    private EnemySeparationBatch batch;
    private string suiteId;
    private bool profiled;
    private bool finished;
    private double warmupStart = -1;
    private double sampledSeconds;
    private bool measuring;
    private int previousVSync;
    private int previousTargetFrameRate;
    private bool changedFrameSettings;

    [MenuItem(MenuRoot + "Original/100 enemigos")]
    private static void Original100() => EnterBenchmark(100, EnemyBenchmarkMethod.Original);
    [MenuItem(MenuRoot + "Original/500 enemigos")]
    private static void Original500() => EnterBenchmark(500, EnemyBenchmarkMethod.Original);
    [MenuItem(MenuRoot + "Original/1000 enemigos")]
    private static void Original1000() => EnterBenchmark(1000, EnemyBenchmarkMethod.Original);

    [MenuItem(MenuRoot + "Secuencial por lotes/100 enemigos")]
    private static void Sequential100() => EnterBenchmark(100, EnemyBenchmarkMethod.Sequential);
    [MenuItem(MenuRoot + "Secuencial por lotes/500 enemigos")]
    private static void Sequential500() => EnterBenchmark(500, EnemyBenchmarkMethod.Sequential);
    [MenuItem(MenuRoot + "Secuencial por lotes/1000 enemigos")]
    private static void Sequential1000() => EnterBenchmark(1000, EnemyBenchmarkMethod.Sequential);

    [MenuItem(MenuRoot + "Jobs/100 enemigos")]
    private static void Jobs100() => EnterBenchmark(100, EnemyBenchmarkMethod.Jobs);
    [MenuItem(MenuRoot + "Jobs/500 enemigos")]
    private static void Jobs500() => EnterBenchmark(500, EnemyBenchmarkMethod.Jobs);
    [MenuItem(MenuRoot + "Jobs/1000 enemigos")]
    private static void Jobs1000() => EnterBenchmark(1000, EnemyBenchmarkMethod.Jobs);

    [MenuItem(MenuRoot + "Jobs + Burst/100 enemigos")]
    private static void Burst100() => EnterBenchmark(100, EnemyBenchmarkMethod.JobsBurst);
    [MenuItem(MenuRoot + "Jobs + Burst/500 enemigos")]
    private static void Burst500() => EnterBenchmark(500, EnemyBenchmarkMethod.JobsBurst);
    [MenuItem(MenuRoot + "Jobs + Burst/1000 enemigos")]
    private static void Burst1000() => EnterBenchmark(1000, EnemyBenchmarkMethod.JobsBurst);

    [MenuItem(MenuRoot + "ECS + Jobs + Burst/100 enemigos")]
    private static void Ecs100() => EnterBenchmark(100, EnemyBenchmarkMethod.Ecs);
    [MenuItem(MenuRoot + "ECS + Jobs + Burst/500 enemigos")]
    private static void Ecs500() => EnterBenchmark(500, EnemyBenchmarkMethod.Ecs);
    [MenuItem(MenuRoot + "ECS + Jobs + Burst/1000 enemigos")]
    private static void Ecs1000() => EnterBenchmark(1000, EnemyBenchmarkMethod.Ecs);

    [MenuItem(MenuRoot + "Medir todos los modos y cargas")]
    private static void MeasureAll() => BeginSuite(false);

    public static void RunSuiteFromCommandLine()
    {
        EditorSceneManager.OpenScene(MainScene);
        BeginSuite(true);
    }

    [InitializeOnLoadMethod]
    private static void RegisterSuiteCallback()
    {
        EditorApplication.playModeStateChanged -= ContinueSuite;
        EditorApplication.playModeStateChanged += ContinueSuite;
    }

    private static void BeginSuite(bool quitWhenFinished)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !string.IsNullOrEmpty(SessionState.GetString(Suite, "")))
        {
            Debug.LogWarning("Benchmark: detené Play antes de iniciar otra medición.");
            return;
        }

        SessionState.SetString(Suite, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));
        SessionState.SetInt(SuiteIndex, 0);
        SessionState.SetBool(SuiteQuit, quitWhenFinished);
        StartSuiteRun();
    }

    private static void StartSuiteRun()
    {
        int index = SessionState.GetInt(SuiteIndex, 0);
        if (index == Loads.Length * Methods.Length)
        {
            FinishSuite(true);
            return;
        }

        SessionState.SetBool(SuiteSucceeded, false);
        if (!EnterBenchmark(Loads[index / Methods.Length], Methods[index % Methods.Length]))
            FinishSuite(false);
    }

    private static void ContinueSuite(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || string.IsNullOrEmpty(SessionState.GetString(Suite, "")))
            return;

        if (!SessionState.GetBool(SuiteSucceeded, false))
        {
            FinishSuite(false);
            return;
        }

        SessionState.SetInt(SuiteIndex, SessionState.GetInt(SuiteIndex, 0) + 1);
        EditorApplication.delayCall += StartSuiteRun;
    }

    private static void FinishSuite(bool success)
    {
        bool quit = SessionState.GetBool(SuiteQuit, false);
        SessionState.EraseString(Suite);
        SessionState.EraseInt(SuiteIndex);
        SessionState.EraseBool(SuiteSucceeded);
        SessionState.EraseBool(SuiteQuit);
        SessionState.EraseInt(PendingCount);
        SessionState.EraseInt(PendingMethod);
        if (success)
            Debug.Log("Benchmark: serie completa.");
        else
            Debug.LogWarning("Benchmark: serie interrumpida.");

        if (quit)
            EditorApplication.Exit(success ? 0 : 1);
    }

    private static bool EnterBenchmark(int count, EnemyBenchmarkMethod method)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Benchmark: salí de Play antes de iniciar otra medición.");
            return false;
        }

        if ((method == EnemyBenchmarkMethod.JobsBurst || method == EnemyBenchmarkMethod.Ecs) && !BurstCompiler.IsEnabled)
        {
            Debug.LogWarning("Benchmark: activá Jobs > Burst > Enable Compilation antes de medir Burst.");
            return false;
        }

        Scene scene = SceneManager.GetActiveScene();
        SceneAsset startScene = EditorSceneManager.playModeStartScene;
        if (scene.path != MainScene || SceneManager.sceneCount != 1 || scene.isDirty ||
            (startScene != null && AssetDatabase.GetAssetPath(startScene) != MainScene))
        {
            Debug.LogWarning("Benchmark: abrí únicamente Main Scene, guardá la escena y verificá la escena de inicio de Play.");
            return false;
        }

        SessionState.SetInt(PendingCount, count);
        SessionState.SetInt(PendingMethod, (int)method);
        EditorApplication.EnterPlaymode();
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartRequestedRun()
    {
        int count = SessionState.GetInt(PendingCount, 0);
        var method = (EnemyBenchmarkMethod)SessionState.GetInt(PendingMethod, 0);
        SessionState.EraseInt(PendingCount);
        SessionState.EraseInt(PendingMethod);
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
        runner.Initialize(count, method, enemyPrefab, data, tray.bounds, spawner.transform.position.y);
    }

    private void Initialize(int count, EnemyBenchmarkMethod method, Enemy prefab, EnemyData data, Bounds bounds, float y)
    {
        this.method = method;
        suiteId = SessionState.GetString(Suite, "");
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
            enemy.UseBatchedMovement = method != EnemyBenchmarkMethod.Original;
            enemies[i] = enemy;
        }

        if (method != EnemyBenchmarkMethod.Original)
            batch = new EnemySeparationBatch(enemies, method);

        Debug.Log($"Benchmark: {method} | {count} enemigos | semilla {Seed}. Calentamiento de {WarmupSeconds} s y medición de {SampleSeconds} s.");
    }

    private void Update()
    {
        batch?.UpdateEnemies();
    }

    private void LateUpdate()
    {
        if (finished)
            return;
        profiled |= UnityEngine.Profiling.Profiler.enabled;
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

        finished = true;
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
            if (suiteId.Length > 0)
                EditorApplication.ExitPlaymode();
            return;
        }

        try
        {
            SaveResults();
            if (suiteId.Length > 0)
                SessionState.SetBool(SuiteSucceeded, true);
        }
        finally
        {
            if (suiteId.Length > 0)
                EditorApplication.ExitPlaymode();
        }
    }

    private void SaveResults()
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Benchmarks"));
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string environment = Application.isBatchMode ? "editor-batch" : "editor";
        string mode = method switch
        {
            EnemyBenchmarkMethod.Sequential => "sequential",
            EnemyBenchmarkMethod.Jobs => "jobs",
            EnemyBenchmarkMethod.JobsBurst => "jobs-burst",
            EnemyBenchmarkMethod.Ecs => "ecs",
            _ => "original"
        };
        string path = Path.Combine(directory, $"benchmark-{mode}-{enemies.Length}-{stamp}.csv");
        Directory.CreateDirectory(directory);
        using (var writer = new StreamWriter(path))
        {
            writer.WriteLine("enemies,seed,frame,frame_ms,method,environment,suite,profiled,width,height");
            for (int i = 0; i < frameTimes.Count; i++)
                writer.WriteLine(FormattableString.Invariant(
                    $"{enemies.Length},{Seed},{i + 1},{frameTimes[i]:F4},{mode},{environment},{suiteId},{(profiled ? 1 : 0)},{Screen.width},{Screen.height}"));
        }

        frameTimes.Sort();
        float median = (frameTimes[(frameTimes.Count - 1) / 2] + frameTimes[frameTimes.Count / 2]) * 0.5f;
        float p95 = frameTimes[Mathf.CeilToInt(frameTimes.Count * 0.95f) - 1];
        double mean = sampledSeconds * 1000.0 / frameTimes.Count;
        Debug.Log(FormattableString.Invariant(
            $"Benchmark terminado | {method} | {enemies.Length} enemigos | {frameTimes.Count} cuadros | media {mean:F2} ms | mediana {median:F2} ms | p95 {p95:F2} ms\nCSV: {path}"));
    }

    private void OnDestroy()
    {
        batch?.Dispose();
        if (!changedFrameSettings)
            return;

        QualitySettings.vSyncCount = previousVSync;
        Application.targetFrameRate = previousTargetFrameRate;
    }
}
#endif
