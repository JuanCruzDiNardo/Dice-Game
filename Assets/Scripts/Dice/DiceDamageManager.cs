using System.Collections.Generic;
using UnityEngine;

public class DiceDamageManager : MonoBehaviour
{
    public static DiceDamageManager Instance { get; private set; }

    private readonly List<IDiceDamageModifier> modifiers = new List<IDiceDamageModifier>();

    [Header("Damage")]
    [SerializeField] private int firstThrowDamage = 1;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;

    [Header("Debug")]
    [SerializeField] private int nextThrowDamage = 1;
    [SerializeField] private bool forceNextRoll;
    [SerializeField] private int forcedNextRollValue;

    private DamageContext dmgContext;

    private float throwStartTime;
    private bool throwInProgress;

    public int NextThrowDamage => nextThrowDamage;
    public int FirstThrowDamage => firstThrowDamage;
    public DamageContext DmgContext => dmgContext;
    public IReadOnlyList<IDiceDamageModifier> Modifiers => modifiers;
    public bool ForceNextRoll => forceNextRoll;
    public int ForcedNextRollValue => forcedNextRollValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        nextThrowDamage = firstThrowDamage;
        ResolveThrow(firstThrowDamage);
    }

    public void AddModifier(IDiceDamageModifier modifier)
    {
        if (modifier == null)
            return;

        modifiers.Add(modifier);
    }

    public void RemoveModifier(IDiceDamageModifier modifier)
    {
        if (modifier == null)
            return;

        modifiers.Remove(modifier);
    }

    public void BeginThrow()
    {
        throwStartTime = Time.time;
        throwInProgress = true;
    }

    public void ResolveThrow(int diceValue)
    {
        int previousRoll = nextThrowDamage;
        float throwDuration = throwInProgress ? Time.time - throwStartTime : 0f;

        DamageContext context = new DamageContext
        {
            DiceValue = previousRoll,
            Damage = previousRoll,
            BaseDamage = previousRoll,
            ThrowDuration = throwDuration
        };

        foreach (IDiceDamageModifier modifier in modifiers)
            modifier.Modify(context);

        foreach (IDiceDamageModifier modifier in modifiers)
        {
            if (modifier is IDiceRollStateModifier stateModifier)
                stateModifier.OnRollResolved(previousRoll, diceValue);
        }

        nextThrowDamage = diceValue;
        dmgContext = context;
        throwInProgress = false;

        Debug.Log("Resultado: " + diceValue + " Damage Base: " + context.BaseDamage + " Final Damage: " + context.Damage);
    }

    public void RequestForcedNextRoll(int value)
    {
        forceNextRoll = true;
        forcedNextRollValue = value;
    }

    public bool TryConsumeForcedNextRoll(out int value)
    {
        if (!forceNextRoll)
        {
            value = 0;
            return false;
        }

        value = forcedNextRollValue;

        forceNextRoll = false;
        forcedNextRollValue = 0;

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Enemy enemy))
            return;

        Vector3 knockbackDirection = enemy.transform.position - transform.position;
        enemy.TakeDamage(dmgContext.Damage, knockbackDirection, knockbackForce);
    }
}