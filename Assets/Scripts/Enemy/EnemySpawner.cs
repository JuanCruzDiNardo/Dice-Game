using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemyPrefab;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private bool autoSpawn;

    private Coroutine spawnRoutine;

    private void Start()
    {
        if (autoSpawn)
        {
            spawnRoutine = StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnEnemy();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SpawnEnemy()
    {
        Instantiate(
            enemyPrefab,
            transform.position,
            Quaternion.identity);
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }
    }
}