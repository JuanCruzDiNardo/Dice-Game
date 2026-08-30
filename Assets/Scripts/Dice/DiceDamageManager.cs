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
    [SerializeField] private int previousRoll;
    [SerializeField] private bool forceNextRoll;
    [SerializeField] private int forcedNextRollValue;

    private DamageContext dmgContext;

    private float throwStartTime;
    private bool throwInProgress;

    public int PreviousRoll => previousRoll;
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

        previousRoll = firstThrowDamage;
        PrepareDamageContext();
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

        PrepareDamageContext();

        Debug.Log("Tirada iniciada | Resultado anterior: " + previousRoll + " | Damage Base: " + dmgContext.BaseDamage + " | Final Damage: " + dmgContext.Damage);
    }

    private void PrepareDamageContext()
    {
        DamageContext context = new DamageContext
        {
            DiceValue = previousRoll,
            Damage = previousRoll,
            BaseDamage = previousRoll,
            ThrowDuration = 0f
        };

        foreach (IDiceDamageModifier modifier in modifiers)
            modifier.Modify(context);

        dmgContext = context;
    }

    public void ResolveThrow(int diceValue)
    {
        float throwDuration = throwInProgress ? Time.time - throwStartTime : 0f;

        if (dmgContext != null)
            dmgContext.ThrowDuration = throwDuration;

        foreach (IDiceDamageModifier modifier in modifiers)
        {
            if (modifier is IDiceRollStateModifier stateModifier)
                stateModifier.OnRollResolved(previousRoll, diceValue);
        }

        previousRoll = diceValue;
        throwInProgress = false;

        Debug.Log("Tirada terminada | Resultado: " + diceValue + " | Próximo Damage Base: " + previousRoll);
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

        if (dmgContext == null)
            return;

        Vector3 knockbackDirection = enemy.transform.position - transform.position;
        enemy.TakeDamage(dmgContext.Damage, knockbackDirection, knockbackForce);
    }
}