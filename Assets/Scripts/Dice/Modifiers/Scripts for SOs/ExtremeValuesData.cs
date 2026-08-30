
using UnityEngine;

[CreateAssetMenu(fileName = "ExtremeValues", menuName = "Dice/Modifiers/Damage/Extremos")]
public class ExtremeValuesData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new ExtremeValuesModifier(multiplier);
    }
}