
using UnityEngine;

[CreateAssetMenu(fileName = "StrongCenter", menuName = "Dice/Modifiers/Damage/Centro Fuerte")]
public class StrongCenterData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new StrongCenterModifier(multiplier);
    }
}
