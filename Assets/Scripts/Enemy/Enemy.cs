using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyData data;

    private Nexus nexus;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => data.maxHealth;
    public float DamageToNexus => data.nexusDamage;

    private void Awake()
    {
        nexus = Nexus.Instance;
        currentHealth = data.maxHealth;
    }

    private void Update()
    {
        if (nexus == null || nexus.IsDestroyed())
            return;

        Vector3 direction =
            (nexus.Position - transform.position).normalized;

        transform.position +=
            direction * data.moveSpeed * Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Debug.Log("Enemy dead, dmg taken:" + damage);
            Die();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Nexus"))
            return;

        nexus.TakeDamage(data.nexusDamage);

        Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}