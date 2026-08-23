using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "BAGD/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth = 10f;
    public float moveSpeed = 2f;
    public float nexusDamage = 1f;
}