using UnityEngine;

[CreateAssetMenu(fileName = "Streak", menuName = "Dice/Modifiers/Future/Racha")]
public class StreakData : DiceDamageModifierData
{
    protected override IDiceDamageModifier CreateModifier()
    {
        return new StreakModifier();
    }
}