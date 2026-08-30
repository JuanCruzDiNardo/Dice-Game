using UnityEngine;

[CreateAssetMenu(fileName = "LuckyNumber", menuName = "Dice/Modifiers/Damage/Numero de la Suerte")]
public class LuckyNumberData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 4;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new LuckyNumberModifier(multiplier);
    }
}
