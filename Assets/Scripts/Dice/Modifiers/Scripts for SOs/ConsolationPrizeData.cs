using UnityEngine;

[CreateAssetMenu(fileName = "ConsolationPrize", menuName = "Dice/Modifiers/Future/Premio Consuelo")]
public class ConsolationPrizeData : DiceDamageModifierData
{
    protected override IDiceDamageModifier CreateModifier()
    {
        if (DiceDamageManager.Instance == null)
            return null;

        return new ConsolationPrizeModifier(DiceDamageManager.Instance.RequestForcedNextRoll);
    }
}