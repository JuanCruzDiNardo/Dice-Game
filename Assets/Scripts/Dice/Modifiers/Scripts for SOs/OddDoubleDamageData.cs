using UnityEngine;

[CreateAssetMenu(fileName = "OddDoubleDamage", menuName = "Dice/Modifiers/Damage/Impar Potente")]
public class OddDoubleDamageData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new OddDoubleDamageModifier(multiplier);
    }
}
