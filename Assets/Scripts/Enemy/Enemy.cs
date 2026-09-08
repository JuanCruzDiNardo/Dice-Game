using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyData data;

    [Header("References")]
    [SerializeField] private EnemyVisualController visualController;
    [SerializeField] private EnemyAudio enemyAudio;

    [Header("Separation")]
    [SerializeField, Min(0f)] private float separationRadius = 1f;
    [SerializeField, Min(0f)] private float separationStrength = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField, Min(1)] private int maxNearbyEnemies = 16;

    [Header("Rotation")]
    [SerializeField, Min(1f)] private float rotationSpeed = 8f;

    [Header("Damage")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Transform damagePopupPoint;

    [Header("Tray Bounds")]
    [SerializeField] private BoxCollider trayBounds;
    [SerializeField, Min(0f)] private float trayEdgePadding = 0.4f;

    private Nexus nexus;

    private float currentHealth;
    private float runtimeMaxHealth;
    private float runtimeMoveSpeed;
    private float runtimeNexusDamage;

    private bool isDead;
    private bool isInitialized;

    private Collider[] enemyColliders;
    private Collider[] nearbyColliders;

    private Vector3 overlapFallbackDirection;
    private Vector3 knockbackVelocity;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => runtimeMaxHealth;
    public float DamageToNexus => runtimeNexusDamage;
    public float MoveSpeed => runtimeMoveSpeed;
    public bool IsDead => isDead;
    public EnemyData Data => data;

    public event Action<Enemy> OnDeath;

    private void Awake()
    {
        nexus = Nexus.Instance;

        enemyColliders = GetComponents<Collider>();
        nearbyColliders = new Collider[maxNearbyEnemies];

        if (visualController == null)
            visualController = GetComponentInChildren<EnemyVisualController>();

        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
        overlapFallbackDirection = new Vector3(randomDirection.x, 0f, randomDirection.y);

        OnDeath += HandleDeathVisuals;

        GameObject trayFloor = GameObject.Find("TrayFloor");

        if (trayFloor != null)
            trayBounds = trayFloor.GetComponent<BoxCollider>();

        if (enemyAudio == null)
            enemyAudio = GetComponent<EnemyAudio>();
    }

    private void Start()
    {
        if (!isInitialized && data != null)
            Initialize(data);
    }

    private void OnDestroy()
    {
        OnDeath -= HandleDeathVisuals;
    }

    public void Initialize(EnemyData enemyData)
    {
        Initialize(enemyData, 1f, 1f, 1f);
    }

    public void Initialize(EnemyData enemyData, float healthMultiplier, float damageMultiplier, float speedMultiplier)
    {
        if (enemyData == null)
        {
            Debug.LogError($"{name} received a null EnemyData.");
            return;
        }

        data = enemyData;

        runtimeMaxHealth = data.maxHealth * Mathf.Max(0f, healthMultiplier);
        runtimeNexusDamage = data.nexusDamage * Mathf.Max(0f, damageMultiplier);
        runtimeMoveSpeed = data.moveSpeed * Mathf.Max(0f, speedMultiplier);

        currentHealth = runtimeMaxHealth;

        isDead = false;
        isInitialized = true;
        knockbackVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

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
        Vector3 directionToNexus = nexus.Position - transform.position;
        directionToNexus.y = 0f;

        if (directionToNexus.sqrMagnitude > 0.001f)
            directionToNexus.Normalize();

        if (directionToNexus.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToNexus);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 separationDirection = CalculateSeparation();

        Vector3 movementDirection = directionToNexus + separationDirection * separationStrength;
        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude > 0.001f)
            movementDirection.Normalize();

        Vector3 normalMovement = movementDirection * runtimeMoveSpeed;
        Vector3 finalMovement = normalMovement + knockbackVelocity;
        Vector3 targetPosition = transform.position + finalMovement * Time.deltaTime;

        targetPosition = ClampToTray(targetPosition);

        transform.position = targetPosition;

        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, data.knockbackRecovery * Time.deltaTime);
    }

    private Vector3 ClampToTray(Vector3 position)
    {
        if (trayBounds == null)
            return position;

        Bounds bounds = trayBounds.bounds;

        float minX = bounds.min.x + trayEdgePadding;
        float maxX = bounds.max.x - trayEdgePadding;
        float minZ = bounds.min.z + trayEdgePadding;
        float maxZ = bounds.max.z - trayEdgePadding;

        float originalX = position.x;
        float originalZ = position.z;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);

        if (!Mathf.Approximately(originalX, position.x))
            knockbackVelocity.x = 0f;

        if (!Mathf.Approximately(originalZ, position.z))
            knockbackVelocity.z = 0f;

        return position;
    }

    private Vector3 CalculateSeparation()
    {
        int nearbyCount = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, nearbyColliders, enemyLayer, QueryTriggerInteraction.Collide);

        Vector3 separation = Vector3.zero;
        int validNeighbours = 0;

        for (int i = 0; i < nearbyCount; i++)
        {
            Collider nearbyCollider = nearbyColliders[i];

            if (nearbyCollider == null)
                continue;

            if (!nearbyCollider.TryGetComponent(out Enemy otherEnemy))
                continue;

            if (otherEnemy == this)
                continue;

            if (otherEnemy.IsDead)
                continue;

            Vector3 awayDirection = transform.position - otherEnemy.transform.position;
            awayDirection.y = 0f;

            float sqrDistance = awayDirection.sqrMagnitude;

            if (sqrDistance < 0.0001f)
            {
                separation += overlapFallbackDirection;
                validNeighbours++;
                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);

            if (distance >= separationRadius)
                continue;

            float weight = 1f - distance / separationRadius;

            separation += awayDirection.normalized * weight;
            validNeighbours++;
        }

        if (validNeighbours > 0)
            separation /= validNeighbours;

        return separation;
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, Vector3.zero, 0f);
    }

    public void TakeDamage(float damage, Vector3 knockbackDirection, float knockbackForce)
    {
        if (!isInitialized)
            return;

        if (isDead)
            return;

        if (damage <= 0f)
            return;

        currentHealth -= damage;

        ShowDamagePopup(damage);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die(EnemyDeathType.Dice);
            return;
        }

        if (enemyAudio != null)
            enemyAudio.PlayDamage();

        ApplyKnockback(knockbackDirection, knockbackForce);
    }

    private void ShowDamagePopup(float damage)
    {
        if (damagePopupPrefab == null || damagePopupPoint == null)
            return;

        GameObject popupGO = Instantiate(damagePopupPrefab, damagePopupPoint.position, Quaternion.identity, damagePopupPoint);

        popupGO.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        popupGO.transform.localScale = new Vector3(3f, 3f, 3f);

        DamagePopup popup = popupGO.GetComponent<DamagePopup>();

        if (popup != null)
            popup.Setup(damage);
    }

    private void ApplyKnockback(Vector3 direction, float force)
    {
        if (force <= 0f)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        float finalForce = force / Mathf.Max(0.01f, data.knockbackResistance);
        knockbackVelocity = direction * finalForce;
    }

    // =========================================================
    // NEXUS
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized)
            return;

        if (isDead)
            return;

        if (!other.CompareTag("Nexus"))
            return;

        nexus.TakeDamage(runtimeNexusDamage);
        Die(EnemyDeathType.Nexus);
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die(EnemyDeathType deathType)
    {
        if (isDead)
            return;

        isDead = true;
        knockbackVelocity = Vector3.zero;

        if (enemyAudio != null)
            enemyAudio.PlayDeath(deathType);

        DisableColliders();

        OnDeath?.Invoke(this);
    }

    private void HandleDeathVisuals(Enemy enemy)
    {
        if (visualController != null)
            visualController.PlayDeath();
    }

    private void DisableColliders()
    {
        foreach (Collider enemyCollider in enemyColliders)
            enemyCollider.enabled = false;
    }
}