using System.Collections.Generic;
using UnityEngine;

public class DiceDamageManager : MonoBehaviour
{
    public static DiceDamageManager Instance { get; private set; }

    private readonly List<IDiceDamageModifier> modifiers = new();

    [Header("Debug")]
    [SerializeField]
    private int nextThrowDamage = 1;

    public int NextThrowDamage => nextThrowDamage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AddModifier(new EvenDoubleDamageModifier());
        AddModifier(new CriticalOnSixModifier());
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

    public DamageContext ResolveThrow(int diceValue)
    {
        DamageContext context = new DamageContext
        {
            DiceValue = nextThrowDamage,
            Damage = nextThrowDamage,
            BaseDamage = nextThrowDamage
        };

        foreach (IDiceDamageModifier modifier in modifiers)
        {
            modifier.Modify(context);
        }

        // El resultado real del dado pasa a ser
        // el daño base de la siguiente tirada.
        nextThrowDamage = diceValue;

        Debug.Log("Resultado:" + diceValue + " Damage Base: " + context.BaseDamage + " Final Damage: " + context.Damage);

        return context;
    }

}