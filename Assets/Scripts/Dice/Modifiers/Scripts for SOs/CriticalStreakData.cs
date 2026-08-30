using UnityEngine;

[CreateAssetMenu(fileName = "CriticalStreak", menuName = "Dice/Modifiers/Future/Racha Critica")]
public class CriticalStreakData : DiceDamageModifierData
{
    [SerializeField] private int multiplier = 2;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new CriticalStreakModifier(multiplier);
    }
}
