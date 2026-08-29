using UnityEngine;

[CreateAssetMenu(fileName = "Accumulation", menuName = "Dice/Modifiers/Future/Acumulacion")]

public class AccumulationData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new AccumulationModifier(multiplier);
    }
}