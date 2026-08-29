using UnityEngine;

// =========================================================
// BASE
// =========================================================

public abstract class DiceModifierData : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string modifierName;

    [TextArea]
    [SerializeField] private string description;

    [SerializeField] private Sprite icon;

    public string ModifierName => modifierName;
    public string Description => description;
    public Sprite Icon => icon;

    public abstract void ApplyModifier();
}

// =========================================================
// DAMAGE BASE
// =========================================================

public abstract class DiceDamageModifierData : DiceModifierData
{
    protected abstract IDiceDamageModifier CreateModifier();

    public override void ApplyModifier()
    {
        if (DiceDamageManager.Instance == null)
        {
            Debug.LogWarning("No existe un DiceDamageManager activo.");
            return;
        }

        IDiceDamageModifier modifier = CreateModifier();

        if (modifier == null)
            return;

        DiceDamageManager.Instance.AddModifier(modifier);
    }
}

// =========================================================
// FACE BASE
// =========================================================

public abstract class DiceFaceModifierData : DiceModifierData
{
    protected DiceFaceManager FaceManager => DiceFaceManager.Instance;

    protected bool HasFaceManager()
    {
        if (FaceManager != null)
            return true;

        Debug.LogWarning("No existe un DiceFaceManager activo.");
        return false;
    }
}

// =========================================================
// DAMAGE
// =========================================================

[CreateAssetMenu(fileName = "EvenDoubleDamage", menuName = "Dice/Modifiers/Damage/Par Potente")]
public class EvenDoubleDamageData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new EvenDoubleDamageModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "CriticalOnSix", menuName = "Dice/Modifiers/Damage/Seis Critico")]
public class CriticalOnSixData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 3;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new CriticalOnSixModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "Compensation", menuName = "Dice/Modifiers/Damage/Compensacion")]
public class CompensationData : DiceDamageModifierData
{
    [SerializeField] private int threshold = 3;
    [SerializeField] private int bonusDamage = 10;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new CompensationModifier(threshold, bonusDamage);
    }
}

[CreateAssetMenu(fileName = "OddDoubleDamage", menuName = "Dice/Modifiers/Damage/Impar Potente")]
public class OddDoubleDamageData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new OddDoubleDamageModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "ExtremeValues", menuName = "Dice/Modifiers/Damage/Extremos")]
public class ExtremeValuesData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new ExtremeValuesModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "StrongCenter", menuName = "Dice/Modifiers/Damage/Centro Fuerte")]
public class StrongCenterData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new StrongCenterModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "StaircaseDamage", menuName = "Dice/Modifiers/Damage/Escalera")]
public class StaircaseDamageData : DiceDamageModifierData
{
    [SerializeField] private int damagePerPoint = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new StaircaseDamageModifier(damagePerPoint);
    }
}

[CreateAssetMenu(fileName = "MinimumDamage", menuName = "Dice/Modifiers/Damage/Piso de Dano")]
public class MinimumDamageData : DiceDamageModifierData
{
    [SerializeField] private int minimumDamage = 4;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new MinimumDamageModifier(minimumDamage);
    }
}

[CreateAssetMenu(fileName = "AllOrNothingDamage", menuName = "Dice/Modifiers/Damage/Todo o Nada")]
public class AllOrNothingDamageData : DiceDamageModifierData
{
    [SerializeField] private float lowMultiplier = 0.5f;
    [SerializeField] private int highMultiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new AllOrNothingModifier(lowMultiplier, highMultiplier);
    }
}

[CreateAssetMenu(fileName = "LuckyNumber", menuName = "Dice/Modifiers/Damage/Numero de la Suerte")]
public class LuckyNumberData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 4;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new LuckyNumberModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "LastHit", menuName = "Dice/Modifiers/Damage/Ultimo Golpe")]
public class LastHitData : DiceDamageModifierData
{
    [SerializeField] private float secondsPerBonus = 1f;
    [SerializeField] private int bonusDamagePerInterval = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new LastHitModifier(secondsPerBonus, bonusDamagePerInterval);
    }
}

// =========================================================
// FUTURE ROLLS
// =========================================================

[CreateAssetMenu(fileName = "ConsolationPrize", menuName = "Dice/Modifiers/Future/Premio Consuelo")]
public class ConsolationPrizeData : DiceDamageModifierData
{
    protected override IDiceDamageModifier CreateModifier()
    {
        if (DiceDamageManager.Instance == null)
            return null;

        return new ConsolationPrizeModifier(DiceDamageManager.Instance.RequestForcedNextRoll);
    }
}

[CreateAssetMenu(fileName = "CriticalStreak", menuName = "Dice/Modifiers/Future/Racha Critica")]
public class CriticalStreakData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new CriticalStreakModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "Accumulation", menuName = "Dice/Modifiers/Future/Acumulacion")]
public class AccumulationData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new AccumulationModifier(multiplier);
    }
}

[CreateAssetMenu(fileName = "AscendingStaircase", menuName = "Dice/Modifiers/Future/Escalera Ascendente")]
public class AscendingStaircaseData : DiceDamageModifierData
{
    [SerializeField] private int bonusDamage = 5;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new AscendingStaircaseModifier(bonusDamage);
    }
}

[CreateAssetMenu(fileName = "DescendingStaircase", menuName = "Dice/Modifiers/Future/Escalera Descendente")]
public class DescendingStaircaseData : DiceDamageModifierData
{
    [SerializeField] private int bonusDamage = 5;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new DescendingStaircaseModifier(bonusDamage);
    }
}

[CreateAssetMenu(fileName = "RaiseTheStakes", menuName = "Dice/Modifiers/Future/Aumentar la Apuesta")]
public class RaiseTheStakesData : DiceDamageModifierData
{
    [SerializeField] private int bonusDamage = 10;
    [SerializeField] private int penaltyDamage = 10;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new RaiseTheStakesModifier(bonusDamage, penaltyDamage);
    }
}

[CreateAssetMenu(fileName = "Alternation", menuName = "Dice/Modifiers/Future/Alternancia")]
public class AlternationData : DiceDamageModifierData
{
    protected override IDiceDamageModifier CreateModifier()
    {
        return new AlternationModifier();
    }
}

[CreateAssetMenu(fileName = "CriticalCharge", menuName = "Dice/Modifiers/Future/Carga Critica")]
public class CriticalChargeData : DiceDamageModifierData
{
    [SerializeField] private int multiplierIncrease = 1;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new CriticalChargeModifier(multiplierIncrease);
    }
}

[CreateAssetMenu(fileName = "Memory", menuName = "Dice/Modifiers/Future/Memoria")]
public class MemoryData : DiceDamageModifierData
{
    [SerializeField] private int historySize = 3;
    [SerializeField] private int multiplier = 3;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new MemoryModifier(historySize, multiplier);
    }
}

[CreateAssetMenu(fileName = "Streak", menuName = "Dice/Modifiers/Future/Racha")]
public class StreakData : DiceDamageModifierData
{
    protected override IDiceDamageModifier CreateModifier()
    {
        return new StreakModifier();
    }
}

// =========================================================
// FACE SET MODIFIERS
// =========================================================

[CreateAssetMenu(fileName = "FaceSetModifier", menuName = "Dice/Modifiers/Faces/Aplicar Face Set")]
public class FaceSetModifierData : DiceFaceModifierData
{
    [Header("Face Set")]
    [SerializeField] private DiceFaceSetData faceSet;

    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        if (faceSet == null)
        {
            Debug.LogWarning($"El modificador {name} no tiene un Face Set asignado.");
            return;
        }

        FaceManager.ApplyFaceSet(faceSet);
    }
}

// =========================================================
// FACE TRANSFORMATIONS
// =========================================================

[CreateAssetMenu(fileName = "SafeDice", menuName = "Dice/Modifiers/Faces/Dado Seguro")]
public class SafeDiceData : DiceFaceModifierData
{
    [SerializeField] private int valueToReplace = 1;
    [SerializeField] private int replacementValue = 3;

    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.ReplaceAllValues(valueToReplace, replacementValue);
    }
}

[CreateAssetMenu(fileName = "LoadedDice", menuName = "Dice/Modifiers/Faces/Dado Cargado")]
public class LoadedDiceData : DiceFaceModifierData
{
    [SerializeField] private int loadedValue = 6;

    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.SetRandomFaceToValue(loadedValue);
    }
}

[CreateAssetMenu(fileName = "Sacrifice", menuName = "Dice/Modifiers/Faces/Sacrificio")]
public class SacrificeDiceData : DiceFaceModifierData
{
    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.SacrificeRandomFaces();
    }
}

[CreateAssetMenu(fileName = "CloneFace", menuName = "Dice/Modifiers/Faces/Clonacion")]
public class CloneFaceData : DiceFaceModifierData
{
    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.CloneRandomFace();
    }
}