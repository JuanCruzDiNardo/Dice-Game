using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemyPrefab;

    public Enemy SpawnEnemy(EnemyData enemyData)
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
        enemy.Initialize(enemyData);

        return enemy;
    }
}