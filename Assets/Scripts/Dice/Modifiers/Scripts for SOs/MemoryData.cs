using UnityEngine;

[CreateAssetMenu(fileName = "Memory", menuName = "Dice/Modifiers/Future/Memoria")]
public class MemoryData : DiceDamageModifierData
{
    [SerializeField] private int historySize = 3;
    [SerializeField] private int multiplier = 3;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new MemoryModifier(historySize, multiplier);
    }
}