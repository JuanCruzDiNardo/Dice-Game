using UnityEngine;

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