using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "BAGD/Wave Data")]
public class WaveData : ScriptableObject
{
    [Serializable]
    public class EnemyGroup
    {
        public EnemyData enemyData;

        [Min(1)]
        public int amount = 1;
    }

    [Header("Wave")]
    public float duration = 30f;

    [Header("Enemies")]
    public List<EnemyGroup> enemies = new List<EnemyGroup>();

    public int TotalEnemyCount
    {
        get
        {
            int total = 0;

            foreach (EnemyGroup group in enemies)
            {
                if (group == null)
                    continue;

                total += group.amount;
            }

            return total;
        }
    }
}