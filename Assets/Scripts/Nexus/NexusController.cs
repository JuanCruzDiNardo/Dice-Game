using System;
using UnityEngine;

public class Nexus : MonoBehaviour
{
    public static Nexus Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Modifiers")]
    [SerializeField] private float maxHealthMultiplier = 1f;
    [SerializeField] private float damageTakenMultiplier = 1f;
    [SerializeField] private float healingReceivedMultiplier = 1f;

    [Header("Regeneration")]
    [SerializeField] private bool enableRegeneration;
    [SerializeField] private float regenerationPerSecond = 1f;

    public float MaxHealth => maxHealth * maxHealthMultiplier;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => CurrentHealth / MaxHealth;

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
    }

    private void Update()
    {
        if (enableRegeneration && !isDestroyed)
        {
            Heal(regenerationPerSecond * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed)
            return;

        float finalDamage = amount * damageTakenMultiplier;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0f, currentHealth);

        OnDamaged?.Invoke(finalDamage);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0f)
        {
            DestroyNexus();
        }
    }

    public void Heal(float amount)
    {
        if (isDestroyed)
            return;

        float finalHeal = amount * healingReceivedMultiplier;

        float previousHealth = currentHealth;

        currentHealth += finalHeal;
        currentHealth = Mathf.Min(currentHealth, MaxHealth);

        if (currentHealth > previousHealth)
        {
            OnHealed?.Invoke(currentHealth - previousHealth);
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

    public void SetMaxHealthMultiplier(float multiplier)
    {
        maxHealthMultiplier = multiplier;

        currentHealth = Mathf.Min(currentHealth, MaxHealth);

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetDamageTakenMultiplier(float multiplier)
    {
        damageTakenMultiplier = multiplier;
    }

    public void SetHealingReceivedMultiplier(float multiplier)
    {
        healingReceivedMultiplier = multiplier;
    }

    private void DestroyNexus()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        OnDestroyed?.Invoke();

        Debug.Log("Nexus Destroyed");
    }
}