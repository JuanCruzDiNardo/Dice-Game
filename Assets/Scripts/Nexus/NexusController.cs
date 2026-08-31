using System;
using UnityEngine;
using HandDrawnHealthBar;

[RequireComponent(typeof(Collider))]
public class Nexus : MonoBehaviour
{
    public static Nexus Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI")]
    [SerializeField] private HealthBarController healthBar;

    [Header("Modifiers")]
    [SerializeField] private float maxHealthMultiplier = 1f;
    [SerializeField] private float damageTakenMultiplier = 1f;
    [SerializeField] private float healingReceivedMultiplier = 1f;

    [Header("Regeneration")]
    [SerializeField] private bool enableRegeneration;
    [SerializeField] private float regenerationPerSecond = 1f;

    public float MaxHealth => maxHealth * maxHealthMultiplier;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;

    /// <summary>
    /// Posición cacheada para uso masivo por enemigos.
    /// </summary>
    public Vector3 Position => transform.position;

    public event Action<float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action OnDestroyed;

    private bool isDestroyed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        currentHealth = MaxHealth;

        UpdateHealthBar();
    }

    private void Update()
    {
        if (!enableRegeneration || isDestroyed)
            return;

        Heal(regenerationPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed || amount <= 0f)
            return;

        float finalDamage = amount * damageTakenMultiplier;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0f, currentHealth);

        UpdateHealthBar();

        OnDamaged?.Invoke(finalDamage);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0f)
            DestroyNexus();
    }

    public void Heal(float amount)
    {
        if (isDestroyed || amount <= 0f)
            return;

        float finalHeal = amount * healingReceivedMultiplier;

        float previousHealth = currentHealth;

        currentHealth += finalHeal;
        currentHealth = Mathf.Min(currentHealth, MaxHealth);

        float healedAmount = currentHealth - previousHealth;

        if (healedAmount <= 0f)
            return;

        UpdateHealthBar();

        OnHealed?.Invoke(healedAmount);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetMaxHealthMultiplier(float multiplier)
    {
        maxHealthMultiplier = Mathf.Max(0.01f, multiplier);

        currentHealth = Mathf.Min(currentHealth, MaxHealth);

        UpdateHealthBar();

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetDamageTakenMultiplier(float multiplier)
    {
        damageTakenMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetHealingReceivedMultiplier(float multiplier)
    {
        healingReceivedMultiplier = Mathf.Max(0f, multiplier);
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null)
            return;

        healthBar.SetHealth(currentHealth, MaxHealth);
    }

    private void DestroyNexus()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        UpdateHealthBar();

        OnDestroyed?.Invoke();

        Debug.Log("Nexus Destroyed");
    }
}