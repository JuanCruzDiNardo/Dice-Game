using UnityEngine;

[CreateAssetMenu(fileName = "EvenDoubleDamage", menuName = "Dice/Modifiers/Damage/Par Potente")]
public class EvenDoubleDamageData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new EvenDoubleDamageModifier(multiplier);
    }
}