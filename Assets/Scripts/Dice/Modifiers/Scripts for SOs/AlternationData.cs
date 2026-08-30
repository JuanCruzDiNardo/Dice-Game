using UnityEngine;

[CreateAssetMenu(fileName = "Alternation", menuName = "Dice/Modifiers/Future/Alternancia")]
public class AlternationData : DiceDamageModifierData
{
    protected override IDiceDamageModifier CreateModifier()
    {
        return new AlternationModifier();
    }
}