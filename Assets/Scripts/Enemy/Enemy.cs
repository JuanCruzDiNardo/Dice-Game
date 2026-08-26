using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyData data;

    [Header("References")]
    [SerializeField] private EnemyVisualController visualController;

    [Header("Separation")]
    [SerializeField, Min(0f)]
    private float separationRadius = 1f;

    [SerializeField, Min(0f)]
    private float separationStrength = 1.5f;

    [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField, Min(1)]
    private int maxNearbyEnemies = 16;

    private Nexus nexus;

    private float currentHealth;
    private bool isDead;

    private Collider[] enemyColliders;
    private Collider[] nearbyColliders;

    // Dirección utilizada únicamente si dos enemigos
    // están exactamente en la misma posición.
    private Vector3 overlapFallbackDirection;

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

        nearbyColliders =
            new Collider[maxNearbyEnemies];

        if (visualController == null)
        {
            visualController = GetComponentInChildren<EnemyVisualController>();
        }

        // Cada enemigo recibe una dirección aleatoria
        // sobre el plano XZ.
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;

        overlapFallbackDirection = new Vector3(randomDirection.x,0f,randomDirection.y);

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

        Move();
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void Move()
    {
        Vector3 directionToNexus =
            nexus.Position - transform.position;

        // Movimiento solamente sobre XZ.
        directionToNexus.y = 0f;

        if (directionToNexus.sqrMagnitude > 0.001f)
        {
            directionToNexus.Normalize();
        }

        Vector3 separationDirection = CalculateSeparation();

        Vector3 finalDirection = directionToNexus + separationDirection * separationStrength;

        finalDirection.y = 0f;

        if (finalDirection.sqrMagnitude > 0.001f)
        {
            finalDirection.Normalize();
        }

        transform.position += finalDirection * data.moveSpeed * Time.deltaTime;
    }

    private Vector3 CalculateSeparation()
    {
        int nearbyCount = Physics.OverlapSphereNonAlloc( transform.position, separationRadius, nearbyColliders, enemyLayer, QueryTriggerInteraction.Collide );

        Vector3 separation = Vector3.zero;

        int validNeighbours = 0;

        for (int i = 0; i < nearbyCount; i++)
        {
            Collider nearbyCollider = nearbyColliders[i];

            if (nearbyCollider == null)
                continue;

            if (!nearbyCollider.TryGetComponent( out Enemy otherEnemy))
                continue;

            // Ignorarse a sí mismo.
            if (otherEnemy == this)
                continue;

            if (otherEnemy.IsDead)
                continue;

            Vector3 awayDirection = transform.position - otherEnemy.transform.position;

            // Ignoramos la altura.
            awayDirection.y = 0f;

            float sqrDistance = awayDirection.sqrMagnitude;

            // Si ambos están prácticamente en el mismo punto
            // usamos una dirección de emergencia.
            if (sqrDistance < 0.0001f)
            {
                separation += overlapFallbackDirection;

                validNeighbours++;

                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);

            if (distance >= separationRadius)
                continue;

            // Cuanto más cerca esté el otro enemigo,
            // mayor será la fuerza de separación.
            float weight = 1f - (distance / separationRadius);

            separation += awayDirection.normalized * weight;

            validNeighbours++;
        }

        if (validNeighbours > 0)
        {
            separation /= validNeighbours;
        }

        return separation;
    }

    // =========================================================
    // DAMAGE
    // =========================================================

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

    // =========================================================
    // NEXUS
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        if (!other.CompareTag("Nexus"))
            return;

        nexus.TakeDamage( data.nexusDamage );

        Die();
    }

    // =========================================================
    // DEATH
    // =========================================================

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
        foreach (Collider enemyCollider in enemyColliders)
        {
            enemyCollider.enabled = false;
        }
    }
}