using UnityEngine;


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