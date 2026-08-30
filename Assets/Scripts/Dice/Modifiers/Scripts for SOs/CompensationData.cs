using UnityEngine;

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