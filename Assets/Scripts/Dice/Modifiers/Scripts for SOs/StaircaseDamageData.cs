using UnityEngine;


[CreateAssetMenu(fileName = "StaircaseDamage", menuName = "Dice/Modifiers/Damage/Escalera")]
public class StaircaseDamageData : DiceDamageModifierData
{
    [SerializeField] private int damagePerPoint = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new StaircaseDamageModifier(damagePerPoint);
    }
}