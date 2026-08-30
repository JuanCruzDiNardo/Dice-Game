using UnityEngine;

[CreateAssetMenu(fileName = "CriticalOnSix", menuName = "Dice/Modifiers/Damage/Seis Critico")]
public class CriticalOnSixData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 3;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new CriticalOnSixModifier(multiplier);
    }
}