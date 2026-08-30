using UnityEngine;

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