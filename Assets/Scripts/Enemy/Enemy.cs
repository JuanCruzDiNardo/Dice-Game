using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyData data;

    [Header("References")]
    [SerializeField] private EnemyVisualController visualController;

    private Nexus nexus;

    private float currentHealth;
    private bool isDead;

    private Collider[] enemyColliders;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => data.maxHealth;
    public float DamageToNexus => data.nexusDamage;
    public bool IsDead => isDead;

    public event Action<Enemy> OnDeath;

    private void Awake()
    {
        nexus = Nexus.Instance;

        currentHealth = data.maxHealth;

        enemyColliders = GetComponents<Collider>();

        if (visualController == null)
            visualController =
                GetComponentInChildren<EnemyVisualController>();

        OnDeath += HandleDeathVisuals;
    }

    private void OnDestroy()
    {
        OnDeath -= HandleDeathVisuals;
    }

    private void Update()
    {
        if (isDead)
            return;

        if (nexus == null || nexus.IsDestroyed())
            return;

        Vector3 direction =
            (nexus.Position - transform.position).normalized;

        transform.position +=
            direction * data.moveSpeed * Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        if (damage <= 0f)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        if (!other.CompareTag("Nexus"))
            return;

        nexus.TakeDamage(data.nexusDamage);

        Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        DisableColliders();

        OnDeath?.Invoke(this);
    }

    private void HandleDeathVisuals(Enemy enemy)
    {
        if (visualController != null)
        {
            visualController.PlayDeath();
        }
    }

    private void DisableColliders()
    {
        foreach (Collider collider in enemyColliders)
        {
            collider.enabled = false;
        }
    }
}