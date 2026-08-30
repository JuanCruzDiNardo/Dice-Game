using UnityEngine;


[CreateAssetMenu(fileName = "MinimumDamage", menuName = "Dice/Modifiers/Damage/Piso de Dano")]
public class MinimumDamageData : DiceDamageModifierData
{
    [SerializeField] private int minimumDamage = 4;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new MinimumDamageModifier(minimumDamage);
    }
}