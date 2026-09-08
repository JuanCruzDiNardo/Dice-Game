using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemyPrefab;

    public Enemy SpawnEnemy(EnemyData enemyData)
    {
        return SpawnEnemy(enemyData, 1f, 1f, 1f);
    }

    public Enemy SpawnEnemy(EnemyData enemyData, float healthMultiplier, float damageMultiplier, float speedMultiplier)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"{name} has no Enemy prefab assigned.");
            return null;
        }

        if (enemyData == null)
        {
            Debug.LogError($"{name} received a null EnemyData.");
            return null;
        }

        Enemy enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        enemy.Initialize(enemyData, healthMultiplier, damageMultiplier, speedMultiplier);

        return enemy;
    }
}