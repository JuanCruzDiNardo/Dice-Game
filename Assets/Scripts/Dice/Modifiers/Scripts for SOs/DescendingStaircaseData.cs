using UnityEngine;

[CreateAssetMenu(fileName = "DescendingStaircase", menuName = "Dice/Modifiers/Future/Escalera Descendente")]
public class DescendingStaircaseData : DiceDamageModifierData
{
    [SerializeField] private int bonusDamage = 5;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new DescendingStaircaseModifier(bonusDamage);
    }
}
