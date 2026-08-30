using UnityEngine;

[CreateAssetMenu(fileName = "AscendingStaircase", menuName = "Dice/Modifiers/Future/Escalera Ascendente")]
public class AscendingStaircaseData : DiceDamageModifierData
{
    [SerializeField] private int bonusDamage = 5;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new AscendingStaircaseModifier(bonusDamage);
    }
}