using System.Collections.Generic;
using System;
using UnityEngine;

public interface IDiceDamageModifier
{
    void Modify(DamageContext context);
}

public interface IDiceRollStateModifier
{
    void OnRollResolved(int previousRoll, int currentRoll);
}

// =========================================================
// DAMAGE MODIFIERS
// =========================================================

public class EvenDoubleDamageModifier : IDiceDamageModifier
{
    private readonly int multiplier;

    public EvenDoubleDamageModifier(int multiplier = 2)
    {
        this.multiplier = multiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue % 2 == 0)
            context.Damage *= multiplier;
    }
}

public class CriticalOnSixModifier : IDiceDamageModifier
{
    private readonly int criticalMultiplier;

    public CriticalOnSixModifier(int criticalMultiplier = 3)
    {
        this.criticalMultiplier = criticalMultiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue == 6)
            context.Damage *= criticalMultiplier;
    }
}

public class CompensationModifier : IDiceDamageModifier
{
    private readonly int threshold;
    private readonly int bonusDamage;

    public CompensationModifier(int threshold = 3, int bonusDamage = 10)
    {
        this.threshold = threshold;
        this.bonusDamage = bonusDamage;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue < threshold)
            context.Damage += bonusDamage;
    }
}

public class OddDoubleDamageModifier : IDiceDamageModifier
{
    private readonly int multiplier;

    public OddDoubleDamageModifier(int multiplier = 2)
    {
        this.multiplier = multiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue % 2 != 0)
            context.Damage *= multiplier;
    }
}



public class ExtremeValuesModifier : IDiceDamageModifier
{
    private readonly int multiplier;

    public ExtremeValuesModifier(int multiplier = 2)
    {
        this.multiplier = multiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue == 1 || context.DiceValue == 6)
            context.Damage *= multiplier;
    }
}

public class StrongCenterModifier : IDiceDamageModifier
{
    private readonly int multiplier;

    public StrongCenterModifier(int multiplier = 2)
    {
        this.multiplier = multiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue == 3 || context.DiceValue == 4)
            context.Damage *= multiplier;
    }
}

public class StaircaseDamageModifier : IDiceDamageModifier
{
    private readonly int damagePerPoint;

    public StaircaseDamageModifier(int damagePerPoint = 2)
    {
        this.damagePerPoint = damagePerPoint;
    }

    public void Modify(DamageContext context)
    {
        context.Damage += context.DiceValue * damagePerPoint;
    }
}

public class MinimumDamageModifier : IDiceDamageModifier
{
    private readonly int minimumDamage;

    public MinimumDamageModifier(int minimumDamage = 4)
    {
        this.minimumDamage = minimumDamage;
    }

    public void Modify(DamageContext context)
    {
        context.Damage = Mathf.Max(context.Damage, minimumDamage);
    }
}

public class AllOrNothingModifier : IDiceDamageModifier
{
    private readonly int highMultiplier;
    private readonly float lowMultiplier;

    public AllOrNothingModifier(float lowMultiplier = 0.5f, int highMultiplier = 2)
    {
        this.lowMultiplier = lowMultiplier;
        this.highMultiplier = highMultiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue <= 3)
            context.Damage = Mathf.CeilToInt(context.Damage * lowMultiplier);
        else
            context.Damage *= highMultiplier;
    }
}

public class LuckyNumberModifier : IDiceDamageModifier
{
    private readonly int luckyNumber;
    private readonly int multiplier;

    public LuckyNumberModifier(int multiplier = 4)
    {
        this.multiplier = multiplier;
        luckyNumber = UnityEngine.Random.Range(1, 7);

        Debug.Log("Número de la suerte: " + luckyNumber);
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue == luckyNumber)
            context.Damage *= multiplier;
    }
}

public class LastHitModifier : IDiceDamageModifier
{
    private readonly float secondsPerBonus;
    private readonly int bonusDamagePerInterval;

    public LastHitModifier(float secondsPerBonus = 1f, int bonusDamagePerInterval = 2)
    {
        this.secondsPerBonus = secondsPerBonus;
        this.bonusDamagePerInterval = bonusDamagePerInterval;
    }

    public void Modify(DamageContext context)
    {
        if (secondsPerBonus <= 0f)
            return;

        int intervals = Mathf.FloorToInt(context.ThrowDuration / secondsPerBonus);
        context.Damage += intervals * bonusDamagePerInterval;
    }
}

// =========================================================
// FUTURE / STATE MODIFIERS
// =========================================================

public class ConsolationPrizeModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private readonly Action<int> forceNextRoll;

    public ConsolationPrizeModifier(Action<int> forceNextRoll)
    {
        this.forceNextRoll = forceNextRoll;
    }

    public void Modify(DamageContext context)
    {
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        if (currentRoll == 1)
            forceNextRoll?.Invoke(6);
    }
}

public class CriticalStreakModifier : IDiceDamageModifier
{
    private readonly int multiplier;

    public CriticalStreakModifier(int multiplier = 2)
    {
        this.multiplier = multiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue == 6)
            context.Damage *= multiplier;
    }
}

public class AccumulationModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private readonly int multiplier;
    private int nextMultiplier = 1;

    public AccumulationModifier(int multiplier = 2)
    {
        this.multiplier = multiplier;
    }

    public void Modify(DamageContext context)
    {
        context.Damage *= nextMultiplier;
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        nextMultiplier = currentRoll == previousRoll ? multiplier : 1;
    }
}

public class AscendingStaircaseModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private readonly int bonusDamage;
    private int nextBonus;

    public AscendingStaircaseModifier(int bonusDamage = 5)
    {
        this.bonusDamage = bonusDamage;
    }

    public void Modify(DamageContext context)
    {
        context.Damage += nextBonus;
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        nextBonus = currentRoll > previousRoll ? bonusDamage : 0;
    }
}

public class DescendingStaircaseModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private readonly int bonusDamage;
    private int nextBonus;

    public DescendingStaircaseModifier(int bonusDamage = 5)
    {
        this.bonusDamage = bonusDamage;
    }

    public void Modify(DamageContext context)
    {
        context.Damage += nextBonus;
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        nextBonus = currentRoll < previousRoll ? bonusDamage : 0;
    }
}

public class RaiseTheStakesModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private readonly int bonusDamage;
    private readonly int penaltyDamage;
    private int nextDamageModifier;

    public RaiseTheStakesModifier(int bonusDamage = 10, int penaltyDamage = 10)
    {
        this.bonusDamage = bonusDamage;
        this.penaltyDamage = penaltyDamage;
    }

    public void Modify(DamageContext context)
    {
        context.Damage += nextDamageModifier;
        context.Damage = Mathf.Max(0, context.Damage);
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        if (currentRoll > previousRoll)
            nextDamageModifier = bonusDamage;
        else if (currentRoll < previousRoll)
            nextDamageModifier = -penaltyDamage;
        else
            nextDamageModifier = 0;
    }
}

public class AlternationModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private int alternationMultiplier = 1;

    public void Modify(DamageContext context)
    {
        context.Damage *= alternationMultiplier;
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        bool previousEven = previousRoll % 2 == 0;
        bool currentEven = currentRoll % 2 == 0;

        if (previousEven != currentEven)
            alternationMultiplier++;
        else
            alternationMultiplier = 1;
    }
}

public class CriticalChargeModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private readonly int multiplierIncrease;
    private int misses;
    private int nextSixMultiplier = 1;

    public CriticalChargeModifier(int multiplierIncrease = 1)
    {
        this.multiplierIncrease = multiplierIncrease;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue == 6)
            context.Damage *= nextSixMultiplier;
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        if (currentRoll == 6)
        {
            nextSixMultiplier = 1 + misses * multiplierIncrease;
            misses = 0;
        }
        else
        {
            misses++;
        }
    }
}

public class MemoryModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private readonly int historySize;
    private readonly int multiplier;
    private readonly Queue<int> rollHistory = new Queue<int>();

    private int nextMultiplier = 1;

    public MemoryModifier(int historySize = 3, int multiplier = 3)
    {
        this.historySize = historySize;
        this.multiplier = multiplier;
    }

    public void Modify(DamageContext context)
    {
        context.Damage *= nextMultiplier;
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        nextMultiplier = rollHistory.Contains(currentRoll) ? 1 : multiplier;

        rollHistory.Enqueue(currentRoll);

        while (rollHistory.Count > historySize)
            rollHistory.Dequeue();
    }
}

public class StreakModifier : IDiceDamageModifier, IDiceRollStateModifier
{
    private int lastRoll = -1;
    private int streak = 1;

    public void Modify(DamageContext context)
    {
        context.Damage *= streak;
    }

    public void OnRollResolved(int previousRoll, int currentRoll)
    {
        if (currentRoll == lastRoll)
            streak++;
        else
            streak = 1;

        lastRoll = currentRoll;
    }
}
